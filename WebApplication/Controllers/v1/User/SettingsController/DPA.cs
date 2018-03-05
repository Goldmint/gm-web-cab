using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Goldmint.Common;

namespace Goldmint.WebApplication.Controllers.v1.User {
	
	/*

	public partial class SettingsController : BaseController {

		// TODO: move to app settings
		private static readonly TimeSpan AllowedPeriodBetweenDPARequests = TimeSpan.FromMinutes(30);

		/// <summary>
		/// Get DPA status
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("dpa/status")]
		[ProducesResponseType(typeof(DpaStatusView), 200)]
		public async Task<APIResponse> DpaStatus() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeDpaStatusView(user));
		}

		/// <summary>
		/// Resend DPA 
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("dpa/resend")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> DpaResend() {
			var user = await GetUserFromDb();

			// check
			var status = MakeDpaStatusView(user);
			if (!status.CanResend) {
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			// resend
			await Core.UserAccount.ResendUserDpaDocument(
				services: HttpContext.RequestServices,
				user: user,
				email: user.Email,
				redirectUrl: this.MakeLink(fragment: AppConfig.AppRoutes.DpaSigned)
			);

			return APIResponse.Success();
		}

		// ---

		[NonAction]
		private DpaStatusView MakeDpaStatusView(DAL.Models.Identity.User user) {
			return new DpaStatusView() {
				IsSigned = user.UserOptions.DPADocument?.IsSigned ?? false,
				CanResend = 
					!(user.UserOptions.DPADocument?.IsSigned ?? false) && // not signed
					(
						user.UserOptions.DPADocument == null || // has not been sent previously
						(DateTime.UtcNow - user.UserOptions.DPADocument.TimeCreated) >= AllowedPeriodBetweenDPARequests // valid resending rate
					),
			};
		}
	}
	
	*/
}
