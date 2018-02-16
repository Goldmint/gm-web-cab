using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.RegisterModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	[Route("api/v1/register")]
	public class RegisterController : BaseController {

		/// <summary>
		/// Start registration flow with email and password
		/// </summary>
		[AreaAnonymous]
		[HttpPost, Route("register")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Register([FromBody] RegisterModel model) {

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

			var result = await Core.UserAccount.CreateUserAccount(HttpContext.RequestServices, model.Email, model.Password);

			if (result.User != null) {

				// confirmation token
				var token = Core.Tokens.JWT.CreateSecurityToken(
					appConfig: AppConfig,
					id: result.User.UserName,
					securityStamp: result.User.AccessStampWeb,
					area: Common.JwtArea.Registration, 
					validFor: TimeSpan.FromDays(90)
				);

				var callbackUrl = this.MakeLink(fragment: AppConfig.AppRoutes.SignUpConfirmation.Replace(":token", token));

				// email confirmation
				await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.EmailConfirmation))
					.Link(callbackUrl)
					.Send(model.Email, "", EmailQueue)
				;

				return APIResponse.Success();
			}
			else {
				if (result.IsUsernameExists || result.IsEmailExists) {
					return APIResponse.BadRequest(APIErrorCode.AccountEmailTaken, "Email is already taken");
				}
			}

			throw new Exception("Registration failed");
		}

		/// <summary>
		/// Email confirmation while registration
		/// </summary>
		[AreaAnonymous]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = (DAL.Models.Identity.User)null;
			var agent = GetUserAgentInfo();

			// check token
			if (! await Core.Tokens.JWT.IsValid(
				AppConfig, 
				model.Token, 
				Common.JwtArea.Registration,
				async (jwt, id) => {
					user = await UserManager.FindByNameAsync(id);
					return user?.AccessStampWeb;
				}
			) || user == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			user.EmailConfirmed = true;
			user.AccessStampWeb = Core.UserAccount.GenerateAccessStamp();
			await DbContext.SaveChangesAsync();

			return APIResponse.Success();
		}

	}
}
