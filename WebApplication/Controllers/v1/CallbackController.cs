using Goldmint.Common;
using Goldmint.CoreLogic.Services.Google.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.WebApplication.Core.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/callback")]
	public class CallbackController : BaseController {

		/// <summary>
		/// Redirect via GET request
		/// </summary>
		[AnonymousAccess]
		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpGet, Route("redirect", Name = "CallbackRedirect")]
		public IActionResult RedirectGet(string to) {
			if (to != null) {
				to = System.Web.HttpUtility.UrlDecode(to);
				return Redirect(to);
			}
			return LocalRedirect("/");
		}

		/// <summary>
		/// Redirect via POST request
		/// </summary>
		[AnonymousAccess]
		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpPost, Route("redirect", Name = "CallbackRedirect")]
		public IActionResult RedirectPost(string to) {
			if (to != null) {
				to = System.Web.HttpUtility.UrlDecode(to);
				return Redirect(to);
			}
			return LocalRedirect("/");
		}

		/// <summary>
		/// Callback from ShuftiPro service. This is not user redirect url
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("shuftipro", Name = "CallbackShuftiPro")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> ShuftiPro() {

			//if (secret == AppConfig.Services.ShuftiPro.CallbackSecret) {

				var check = await KycExternalProvider.OnServiceCallback(HttpContext.Request);
				if (check.IsFinalStatus) {

					var ticket = await (
						from t in DbContext.KycShuftiProTicket
						where t.ReferenceId == check.TicketId && t.TimeResponded == null
						select t
					)
						.Include(tickt => tickt.User)
						.ThenInclude(user => user.UserVerification)
						.FirstOrDefaultAsync()
					;

					if (ticket != null) {

						var userVerified = check.OverallStatus == CoreLogic.Services.KYC.VerificationStatus.Verified;

						ticket.IsVerified = userVerified;
						ticket.CallbackStatusCode = check.ServiceStatus;
						ticket.CallbackMessage = check.ServiceMessage;
						ticket.TimeResponded = DateTime.UtcNow;

						await DbContext.SaveChangesAsync();

						if (GoogleSheets != null && ticket?.User?.UserVerification != null) {
							try {
								await GoogleSheets.InsertUser(
									new UserInfoCreate() {
										UserId = ticket.UserId,
										UserName = ticket.User.UserName,
										FirstName = ticket.User.UserVerification.FirstName,
										LastName = ticket.User.UserVerification.LastName,
										Country = ticket.User.UserVerification.Country,
										Birthday = ticket.User.UserVerification.DoB?.ToString("yyyy MMMM dd"),
									}
								);
							}
							catch (Exception e) {
								Logger.Error(e, "Failed to persist user's verification in Google Sheets");
							}
						}
					}
				}
			//}

			return Ok();
		}

		/// <summary>
		/// Callback from SignRequest service
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("signrequest/{secret}")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> SignRequest(string secret) {

			if (secret == AppConfig.Services.SignRequest.CallbackSecret) {

				var check = await DocSigningProvider.OnServiceCallback(HttpContext.Request);
				if (check.OverallStatus == OverallStatus.Signed || check.OverallStatus == OverallStatus.Declined) {

					var doc = await DbContext.SignedDocument
						.Include(_ => _.User)
						.FirstOrDefaultAsync(_ => _.ReferenceId == check.ReferenceId)
					;
					if (doc != null) {
						
						doc.IsSigned = check.OverallStatus == OverallStatus.Signed;
						doc.CallbackEvent = check.ServiceMessage;
						doc.CallbackStatus = check.ServiceStatus;
						doc.TimeCompleted = DateTime.UtcNow;

						await DbContext.SaveChangesAsync();
					}
				}

			}
			return Ok();
		}
	}
}