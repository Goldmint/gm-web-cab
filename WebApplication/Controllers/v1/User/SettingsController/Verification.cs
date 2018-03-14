using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SettingsController : BaseController {

		// TODO: move to app settings
		private static readonly TimeSpan AllowedPeriodBetweenKYCRequests = TimeSpan.FromMinutes(30);
		private static readonly TimeSpan AllowedPeriodBetweenAgreementRequests = TimeSpan.FromMinutes(30);

		/// <summary>
		/// Verification data
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("verification/view")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationView() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// Step 1. Fill verification form
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("verification/edit")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationEdit([FromBody] VerificationEditModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);

			// on tier-0
			if (userTier == UserTier.Tier0) {

				// format phone number
				var phoneFormatted = Common.TextFormatter.NormalizePhoneNumber(model.PhoneNumber);

				// parse dob
				var dob = DateTime.ParseExact(model.Dob, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

				user.UserVerification = new UserVerification() {

					FirstName = model.FirstName.LimitLength(64),
					MiddleName = model.MiddleName?.LimitLength(64),
					LastName = model.LastName.LimitLength(64),
					DoB = dob,

					PhoneNumber = phoneFormatted.LimitLength(32),
					Country = Common.Countries.GetNameByAlpha2(model.Country),
					CountryCode = model.Country.ToUpper(),
					State = model.State.LimitLength(256),
					City = model.City.LimitLength(256),
					PostalCode = model.PostalCode.LimitLength(16),
					Street = model.Street.LimitLength(256),
					Apartment = model.Apartment?.LimitLength(128),

					TimeUserChanged = DateTime.UtcNow,
				};

				DbContext.UserVerification.Add(user.UserVerification);
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// Step 2. KYC verification
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("verification/kycStart")]
		[ProducesResponseType(typeof(VerificationKycStartView), 200)]
		public async Task<APIResponse> VerificationKycStart([FromBody] VerificationKycStartModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);

			// on tier-1 + KYC is not completed
			if (userTier != UserTier.Tier1 || CoreLogic.User.HasKYCVerification(user.UserVerification)) {
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
				DoB = ticket.DoB,
				PhoneNumber = ticket.PhoneNumber,
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

		/// <summary>
		/// Step 3. Resend primary agreement to sign
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("verification/resendAgreement")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationResendAgreement() {

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var userLocale = Locale.En;

			// on tier-1 + KYC completed + agreement is not signed
			if (userTier != UserTier.Tier1 || !CoreLogic.User.HasKYCVerification(user.UserVerification) || CoreLogic.User.HasSignedAgreement(user.UserVerification)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// check previous verification attempt
			var status = MakeVerificationView(user);
			if (status.IsAgreementPending) {
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			// ---

			await Core.UserAccount.ResendUserTosDocument(
				locale: userLocale,
				services: HttpContext.RequestServices,
				user: user,
				email: user.Email,
				redirectUrl: this.MakeLink(fragment: AppConfig.AppRoutes.VerificationPage)
			);

			return APIResponse.Success(MakeVerificationView(user));
		}

		// ---

		[NonAction]
		private VerificationView MakeVerificationView(DAL.Models.Identity.User user) {

			if (user == null) {
				throw new ArgumentException("User must be specified");
			}

			var kycFinished = CoreLogic.User.HasKYCVerification(user.UserVerification);
			var kycPending =
					!kycFinished &&
					user.UserVerification?.LastKycTicket != null &&
					user.UserVerification.LastKycTicket.TimeResponded == null &&
					(DateTime.UtcNow - user.UserVerification.LastKycTicket.TimeCreated) < AllowedPeriodBetweenKYCRequests
				;

			var agrSigned = CoreLogic.User.HasSignedAgreement(user.UserVerification);
			var agrPending =
					!agrSigned &&
					user.UserVerification?.LastAgreement != null &&
					user.UserVerification.LastAgreement.TimeCompleted == null &&
					(DateTime.UtcNow - user.UserVerification.LastAgreement.TimeCreated) < AllowedPeriodBetweenAgreementRequests
				;
			
			var ret = new VerificationView() {

				IsFormFilled = CoreLogic.User.HasFilledPersonalData(user?.UserVerification),

				IsKycPending = kycPending,
				IsKycFinished = kycFinished,

				IsAgreementPending = agrPending,
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

