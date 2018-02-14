using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	public partial class SettingsController : BaseController {

		/// <summary>
		/// Change password
		/// </summary>
		[AreaAuthorized]
		[HttpPost, Route("changePassword")]
		[ProducesResponseType(typeof(ChangePasswordView), 200)]
		public async Task<APIResponse> ChangePassword([FromBody] ChangePasswordModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// first check tfa
			if (user.TwoFactorEnabled && !Core.Tokens.GoogleAuthenticator.Validate(model.TfaCode, user.TFASecret)) {
				return APIResponse.BadRequest(nameof(model.TfaCode), "Invalid 2fa code");
			}

			// check current password
			if (await UserManager.HasPasswordAsync(user) && (model.Current == null || !await UserManager.CheckPasswordAsync(user, model.Current))) {
				return APIResponse.BadRequest(nameof(model.Current), "Invalid current password");
			}

			// set new password
			await UserManager.RemovePasswordAsync(user);
			await UserManager.AddPasswordAsync(user, model.New);

			// posteffect
			{
				// notification
				await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.PasswordChanged))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, EmailQueue)
					;

				// activity
				await CoreLogic.UserAccount.SaveActivity(
					services: HttpContext.RequestServices,
					user: user,
					type: Common.UserActivityType.Password,
					comment: "Password changed",
					ip: agent.Ip,
					agent: agent.Agent
				);
			}

			return APIResponse.Success(
				new ChangePasswordView() {
				}
			);
		}
	}
}
