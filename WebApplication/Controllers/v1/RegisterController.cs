using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.RegisterModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.WebApplication.Core;
using Goldmint.WebApplication.Core.Tokens;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/register")]
	public class RegisterController : BaseController {

		/// <summary>
		/// Start registration flow with email and password
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("register")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Register([FromBody] RegisterModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

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
					entityId: result.User.UserName,
					audience: JwtAudience.Cabinet,
					securityStamp: "",
					area: Common.JwtArea.Registration, 
					validFor: TimeSpan.FromDays(2)
				);

				var callbackUrl = this.MakeAppLink(JwtAudience.Cabinet, fragment: AppConfig.Apps.Cabinet.RouteSignUpConfirmation.Replace(":token", token));

				// email confirmation
				await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.EmailConfirmation, userLocale))
					.Link(callbackUrl)
					.Send(model.Email, "", EmailQueue)
				;

				// auth token
				return APIResponse.Success(
					new Models.API.v1.User.UserModels.AuthenticateView() {
						Token = JWT.CreateAuthToken(
							appConfig: AppConfig, 
							user: result.User, 
							audience: JwtAudience.Cabinet,
							area: JwtArea.Authorized
						),
						TfaRequired = false,
					}
				);
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
		[AnonymousAccess]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = (DAL.Models.Identity.User)null;
			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			// check token
			if (! await Core.Tokens.JWT.IsValid(
				appConfig: AppConfig, 
				jwtToken: model.Token, 
				expectedAudience: JwtAudience.Cabinet,
				expectedArea: Common.JwtArea.Registration,
				validStamp: (jwt, id) => {
					return Task.FromResult("");
				}
			) || user == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			user.EmailConfirmed = true;
			await DbContext.SaveChangesAsync();

			return APIResponse.Success();
		}

	}
}
