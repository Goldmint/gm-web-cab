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

	public partial class SettingsController : BaseController {

		/// <summary>
		/// Change password
		/// </summary>
		[RequireJWTArea(JwtArea.Authorized)]
		[HttpPost, Route("changePassword")]
		[ProducesResponseType(typeof(ChangePasswordView), 200)]
		public async Task<APIResponse> ChangePassword([FromBody] ChangePasswordModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			// first check tfa
			if (user.TwoFactorEnabled && !Core.Tokens.GoogleAuthenticator.Validate(model.TfaCode, user.TfaSecret)) {
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
				await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.PasswordChanged, userLocale))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, user.UserName, EmailQueue)
					;

				// activity
				var userActivity = CoreLogic.User.CreateUserActivity(
					user: user,
					type: Common.UserActivityType.Password,
					comment: "Password changed",
					ip: agent.Ip,
					agent: agent.Agent,
					locale: userLocale
				);
				DbContext.UserActivity.Add(userActivity);
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(
				new ChangePasswordView() {
				}
			);
		}
	}
}
