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

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SettingsController : BaseController {

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
		/// Fill verification form
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

			// one-time form filling
			if (user.UserVerification == null) {

				// format phone number
				var phoneFormatted = Common.TextFormatter.NormalizePhoneNumber(model.PhoneNumber);
				// dob
				var dob = DateTime.ParseExact(model.Dob, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

				user.UserVerification = new UserVerification() {

					FirstName = model.FirstName,
					MiddleName = model.MiddleName,
					LastName = model.LastName,
					DoB = dob,

					PhoneNumber = phoneFormatted,
					Country = Common.Countries.GetNameByAlpha2(model.Country),
					CountryCode = model.Country.ToUpper(),
					State = model.State,
					City = model.City,
					PostalCode = model.PostalCode,
					Street = model.Street,
					Apartment = model.Apartment,

					TimeUserChanged = DateTime.UtcNow,
				};

				DbContext.UserVerification.Add(user.UserVerification);
				await DbContext.SaveChangesAsync();

				// send agreement
				await Core.UserAccount.ResendVerificationPrimaryAgreement(
					services: HttpContext.RequestServices,
					user: user,
					email: user.Email
				);
			}

			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// KYC verification redirect. Level 0 verification required
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

			// check level 0 verirfication
			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user) || CoreLogic.UserAccount.IsUserVerifiedL1(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// net kyc ticket
			var ticket = new KycShuftiProTicket() {
				UserId = user.Id,
				ReferenceId = Guid.NewGuid().ToString("N"),
				Method = "",

				FirstName = user.UserVerification.FirstName,
				LastName = user.UserVerification.LastName,
				DoB = user.UserVerification.DoB.Value,
				CountryCode = user.UserVerification.CountryCode,
				PhoneNumber = user.UserVerification.PhoneNumber,
				TimeCreated = DateTime.UtcNow,
			};
			await DbContext.KycShuftiProTicket.AddAsync(ticket);
			await DbContext.SaveChangesAsync();

			// new redirect
			var kycUser = new CoreLogic.Services.KYC.UserData() {
				FirstName = ticket.FirstName,
				LastName = ticket.LastName,
				CountryCode = ticket.CountryCode,
				DoB = ticket.DoB,
				PhoneNumber = ticket.PhoneNumber,
			};
			var callbackURL = Url.Link("CallbackShuftiPro", new { });
			var userTempRedirectURL = Url.Link("CallbackRedirect", new { to = System.Web.HttpUtility.UrlEncode(model.Redirect) });
			var kycRedirect = await KycExternalProvider.GetRedirect(kycUser, ticket.ReferenceId, userTempRedirectURL, callbackURL);

			Logger.Trace($"{user.UserName} got kyc redirect to {kycRedirect} with callback to {callbackURL} and middle redirect to {userTempRedirectURL}");

			return APIResponse.Success(new VerificationKycStartView() {
				TicketId = ticket.Id.ToString(),
				Redirect = kycRedirect,
			});
		}

		/// <summary>
		/// KYC verification status
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("verification/kycStatus")]
		[ProducesResponseType(typeof(VerificationKycStatusView), 200)]
		public async Task<APIResponse> VerificationKycStatus([FromBody] VerificationKycStatusModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			bool? verified = null;

			var user = await GetUserFromDb();
			if (user != null && long.TryParse(model.TicketId, out var ticketId) && ticketId > 0) {

				// find ticket
				var ticket = await (
					from v in DbContext.KycShuftiProTicket
					where v.Id == ticketId && v.UserId == user.Id
					select v
				)
					.AsNoTracking()
					.FirstOrDefaultAsync()
				;
				if (ticket != null) {
					verified = ticket.IsVerified;
				}
			}

			if (verified != null) {
				return APIResponse.Success(new VerificationKycStatusView() {
					Verified = verified.Value,
				});
			}

			return APIResponse.BadRequest(nameof(model.TicketId), "Ticket not found");
		}

		/// <summary>
		/// Resend primary agreement to sign
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("verification/resendAgreement")]
		[ProducesResponseType(typeof(VerificationResendAgreementView), 200)]
		public async Task<APIResponse> VerificationResendAgreement() {

			var user = await GetUserFromDb();

			// form must be filled
			if (user.UserVerification?.FirstName == null || user.UserVerification?.LastName == null) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			var limitMinutes = 30d;
			var sent = false;
			var nextDate = (long?)null;

			// document is not signed
			if (user.UserVerification?.SignedAgreementId == null) {

				DbContext.Entry(user.UserVerification).Reference(_ => _.LastAgreement).Load();

				// limit
				if (user.UserVerification.LastAgreement != null && (DateTime.UtcNow - user.UserVerification.LastAgreement.TimeCreated).TotalMinutes < limitMinutes) {
					nextDate = 
						((DateTimeOffset)user.UserVerification.LastAgreement.TimeCreated.AddMinutes(limitMinutes))
						.ToUnixTimeSeconds();
				}
				// can send
				else {
					sent = await Core.UserAccount.ResendVerificationPrimaryAgreement(
						services: HttpContext.RequestServices,
						user: user,
						email: user.Email
					);
				}
			}

			return APIResponse.Success(
				new VerificationResendAgreementView() {
					Resent = sent,
					AvailableDate = nextDate,
				}
			);
		}

		// ---

		[NonAction]
		private VerificationView MakeVerificationView(DAL.Models.Identity.User user) {

			var ret = new VerificationView() {

				IsFormFilled = user.UserVerification?.FirstName != null && user.UserVerification?.LastName != null,
				IsAgreementSigned = user.UserVerification?.SignedAgreementId != null,
				IsKYCFinished = user.UserVerification?.KycShuftiProTicketId != null,

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
