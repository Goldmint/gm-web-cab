using Goldmint.Common;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.RestoreModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	[Route("api/v1/restore")]
	public class RestoreController : BaseController {

		/// <summary>
		/// Start password restoration flow
		/// </summary>
		[AreaAnonymous]
		[HttpPost, Route("password")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Password([FromBody] RestoreModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var agent = GetUserAgentInfo();

			// captcha
			if (!HostingEnvironment.IsDevelopment()) {
				if (!await Core.Recaptcha.Verify(AppConfig.Services.Recaptcha.SecretKey, model.Captcha, agent.Ip)) {
					return APIResponse.BadRequest(nameof(model.Captcha), "Failed to validate captcha");
				}
			}

			// try find user
			var user = await UserManager.FindByEmailAsync(model.Email);
			if (user == null || !(await UserManager.IsEmailConfirmedAsync(user))) {
				return APIResponse.Success();
			}

			// confirmation token
			var token = Core.Tokens.JWT.CreateSecurityToken(
				appConfig: AppConfig,
				id: user.UserName,
				securityStamp: user.AccessStamp,
				area: Common.JwtArea.RestorePassword,
				validFor: TimeSpan.FromHours(24)
			);

			var callbackUrl = this.MakeLink(fragment: AppConfig.AppRoutes.PasswordRestoration.Replace(":token", token));

			// restoration email
			await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.PasswordRestoration))
				.Link(callbackUrl)
				.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
				.Send(model.Email, EmailQueue)
			;

			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Password,
				comment: "Password restoration requested",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success();
		}
		
		/// <summary>
		/// New password
		/// </summary>
		[AreaAnonymous]
		[HttpPost, Route("newPassword")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> NewPassword([FromBody] NewPasswordModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = (DAL.Models.Identity.User)null;
			var agent = GetUserAgentInfo();

			// check token
			if (!await Core.Tokens.JWT.IsValid(
				AppConfig,
				model.Token,
				Common.JwtArea.RestorePassword,
				async (jwt, id) => {
					user = await UserManager.FindByNameAsync(id);
					return user?.AccessStamp;
				}
			) || user == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			await UserManager.RemovePasswordAsync(user);
			await UserManager.AddPasswordAsync(user, model.Password);

			user.AccessStamp = Core.UserAccount.GenerateAccessStamp();
			await DbContext.SaveChangesAsync();

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

			return APIResponse.Success();
		}

	}
}