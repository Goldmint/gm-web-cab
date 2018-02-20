using Goldmint.WebApplication.Core.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/callback")]
	public class CallbackController : BaseController {

		/// <summary>
		/// Redirect via GET request
		/// </summary>
		[AreaAnonymous]
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
		[AreaAnonymous]
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
		[AreaAnonymous]
		[HttpPost, Route("shuftipro", Name = "CallbackShuftiPro")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> ShuftiPro() {

			var check = await KycExternalProvider.OnServiceCallback(HttpContext.Request);

			if (check.OverallStatus != CoreLogic.Services.KYC.VerificationStatus.Fail) {

				var ticket = await DbContext.KycShuftiProTicket
					.Include(tickt => tickt.User)
						.ThenInclude(user => user.UserVerification)
					.FirstAsync(tickt => tickt.ReferenceId == check.TicketId)
				;

				if (ticket?.User?.UserVerification != null) {
					var userVerified = check.OverallStatus == CoreLogic.Services.KYC.VerificationStatus.UserVerified;

					ticket.IsVerified = userVerified;
					ticket.CallbackStatusCode = check.ServiceStatus;
					ticket.CallbackMessage = check.ServiceMessage;
					ticket.TimeResponded = DateTime.UtcNow;

					if (userVerified) {
						ticket.User.UserVerification.KycShuftiProTicket = ticket;
					}

					await DbContext.SaveChangesAsync();
				}
			}

			return Ok();
		}

		/// <summary>
		/// Callback from SignRequest service
		/// </summary>
		[AreaAnonymous]
		[HttpPost, Route("signrequest")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public IActionResult SignRequest() {
			return Ok();
		}
	}
}