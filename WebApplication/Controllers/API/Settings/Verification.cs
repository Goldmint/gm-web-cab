using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	public partial class SettingsController : BaseController {

		/// <summary>
		/// Verification data
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpGet, Route("verification/view")]
		[ProducesResponseType(typeof(VerificationView), 200)]
		public async Task<APIResponse> VerificationView() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// Fill verification form
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
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
				// get region info
				var regionInfo = new RegionInfo(model.Country);
				// dob
				var dob = DateTime.ParseExact(model.Dob, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

				user.UserVerification = new UserVerification() {

					FirstName = model.FirstName,
					MiddleName = model.MiddleName,
					LastName = model.LastName,
					DoB = dob,

					PhoneNumber = phoneFormatted,
					Country = regionInfo.EnglishName,
					CountryCode = regionInfo.TwoLetterISORegionName,
					State = model.State,
					City = model.City,
					PostalCode = model.PostalCode,
					Street = model.Street,
					Apartment = model.Apartment,

					TimeUserChanged = DateTime.UtcNow,
				};

				DbContext.Add(user.UserVerification);
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(MakeVerificationView(user));
		}

		/// <summary>
		/// KYC verification redirect. Level 0 verification required
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
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
			var kycRedirect = await KycExternalProvider.GetRedirect(kycUser, ticket.ReferenceId, model.Redirect, callbackURL);

			Logger.Trace($"{user.UserName} got kyc redirect to {kycRedirect} with callback to {callbackURL} and middle redirect to {userTempRedirectURL}");

			return APIResponse.Success(new VerificationKycStartView() {
				TicketId = ticket.Id.ToString(),
				Redirect = kycRedirect,
			});
		}

		/// <summary>
		/// KYC verification status
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
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

		// ---

		[NonAction]
		private VerificationView MakeVerificationView(User user) {

			var ret = new VerificationView() {

				HasVerificationL0 = CoreLogic.UserAccount.IsUserVerifiedL0(user),
				HasVerificationL1 = CoreLogic.UserAccount.IsUserVerifiedL1(user),

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
