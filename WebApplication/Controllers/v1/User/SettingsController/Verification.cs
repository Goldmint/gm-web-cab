using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SettingsController : BaseController {

		private static readonly TimeSpan AllowedPeriodBetweenKycRequests = TimeSpan.FromMinutes(5);

		/// <summary>
		/// Verification data
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("verification/view")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationView() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// Step 1. Agreed with TOS
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("verification/agreedWithTos")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> AgreedWithTos() {

            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var userLocale = GetUserLocale();

			if (userTier == UserTier.Tier0) {
				if (user.UserVerification == null) {
					user.UserVerification = new UserVerification();
				}
				user.UserVerification.AgreedWithTos = true;
				user.UserVerification.TimeUserChanged = DateTime.UtcNow;
				await DbContext.SaveChangesAsync();
			}
			
			return APIResponse.Success();
		}

		/// <summary>
		/// Step 2. Fill verification form
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpPost, Route("verification/edit")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationEdit([FromBody] VerificationEditModel model) {
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);

			if (userTier == UserTier.Tier1 && !CoreLogic.User.HasKycVerification(user.UserVerification)) {

				// format phone number
				var phoneFormatted = Common.TextFormatter.NormalizePhoneNumber(model.PhoneNumber);

				// parse dob
				var dob = DateTime.ParseExact(model.Dob, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

				{
					user.UserVerification.FirstName = model.FirstName.Limit(64);
					user.UserVerification.MiddleName = model.MiddleName?.Limit(64);
					user.UserVerification.LastName = model.LastName.Limit(64);
					user.UserVerification.DoB = dob;

					user.UserVerification.PhoneNumber = phoneFormatted.Limit(32);
					user.UserVerification.Country = Common.Countries.GetNameByAlpha2(model.Country);
					user.UserVerification.CountryCode = model.Country.ToUpper();
					user.UserVerification.State = model.State.Limit(256);
					user.UserVerification.City = model.City.Limit(256);
					user.UserVerification.PostalCode = model.PostalCode.Limit(16);
					user.UserVerification.Street = model.Street.Limit(256);
					user.UserVerification.Apartment = model.Apartment?.Limit(128);

					user.UserVerification.TimeUserChanged = DateTime.UtcNow;
				}

				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// Step 3. KYC verification
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpPost, Route("verification/kycStart")]
		[ProducesResponseType(typeof(VerificationKycStartView), 200)]
		public async Task<APIResponse> VerificationKycStart([FromBody] VerificationKycStartModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);

			// tos not signed, didn't fill personal data, has kyc already
			if (
				!CoreLogic.User.HasTosSigned(user.UserVerification) 
				|| !CoreLogic.User.HasFilledPersonalData(user.UserVerification) 
				|| CoreLogic.User.HasKycVerification(user.UserVerification)
			) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// check previous verification attempt
			var status = MakeVerificationView(user);
			if (status.IsKycPending) {
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			// ---

			// new kyc ticket
			var ticket = new KycTicket() {
				UserId = user.Id,
				ReferenceId = Guid.NewGuid().ToString("N"),
				Method = "general",

				FirstName = user.UserVerification.FirstName,
				LastName = user.UserVerification.LastName,
				DoB = user.UserVerification.DoB.Value,
				CountryCode = user.UserVerification.CountryCode,
				PhoneNumber = user.UserVerification.PhoneNumber,
				TimeCreated = DateTime.UtcNow,
			};
			DbContext.KycShuftiProTicket.Add(ticket);
			await DbContext.SaveChangesAsync();

			// set last ticket
			user.UserVerification.LastKycTicket = ticket;
			await DbContext.SaveChangesAsync();

			// new redirect
			var kycUser = new CoreLogic.Services.KYC.UserData() {
				FirstName = ticket.FirstName,
				LastName = ticket.LastName,
				CountryCode = ticket.CountryCode,
				LanguageCode = GetUserLocale().ToString().ToUpper(),
				DoB = ticket.DoB,
				PhoneNumber = ticket.PhoneNumber,
				Email = user.Email,
			};
			
			var callbackUrl = Url.Link("CallbackShuftiPro", new { /*secret = AppConfig.Services.ShuftiPro.CallbackSecret*/ });
			var userTempRedirectUrl = Url.Link("CallbackRedirect", new { to = System.Web.HttpUtility.UrlEncode(model.Redirect) });
			var kycRedirect = await KycExternalProvider.GetRedirect(
				kycUser,
				ticket.ReferenceId,
				userTempRedirectUrl,
				callbackUrl
			);

			Logger.Trace($"{user.UserName} got kyc redirect to {kycRedirect} with callback to {callbackUrl} and middle redirect to {userTempRedirectUrl}");

			return APIResponse.Success(new VerificationKycStartView() {
				TicketId = ticket.Id.ToString(),
				Redirect = kycRedirect,
			});
		}


		// ---

		[NonAction]
		private VerificationView MakeVerificationView(DAL.Models.Identity.User user) {

			if (user == null) {
				throw new ArgumentException("User must be specified");
			}

			var kycFinished = CoreLogic.User.HasKycVerification(user.UserVerification);
			var kycPending =
					!kycFinished &&
					user.UserVerification?.LastKycTicket != null &&
					user.UserVerification.LastKycTicket.TimeResponded == null &&
					(DateTime.UtcNow - user.UserVerification.LastKycTicket.TimeCreated) < AllowedPeriodBetweenKycRequests
				;

		    var rcfg = RuntimeConfigHolder.Clone();

			var agrSigned = CoreLogic.User.HasTosSigned(user.UserVerification);
			
			var ret = new VerificationView() {

				IsFormFilled = CoreLogic.User.HasFilledPersonalData(user?.UserVerification),

				IsKycPending = kycPending,
				IsKycFinished = kycFinished,

                IsAgreementSigned = agrSigned,

				FirstName = user.UserVerification?.FirstName ?? "",
				MiddleName = user.UserVerification?.MiddleName ?? "",
				LastName = user.UserVerification?.LastName ?? "",
				Dob = user.UserVerification?.DoB?.ToString("dd.MM.yyyy") ?? "",
				PhoneNumber = user.UserVerification?.PhoneNumber ?? "",
				Country = user.UserVerification?.CountryCode ?? "",
				State = user.UserVerification?.State ?? "",
				City = user.UserVerification?.City ?? "",
				PostalCode = user.UserVerification?.PostalCode ?? "",
				Street = user.UserVerification?.Street ?? "",
				Apartment = user.UserVerification?.Apartment ?? "",
			};

			return ret;
		}

	}
}

