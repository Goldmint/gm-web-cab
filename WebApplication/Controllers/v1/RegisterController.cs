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
			var audience = JwtAudience.Cabinet;
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
					audience: audience,
					securityStamp: result.User.JWTSalt,
					area: Common.JwtArea.Registration, 
					validFor: TimeSpan.FromDays(3)
				);

				var callbackUrl = this.MakeAppLink(audience, fragment: AppConfig.Apps.Cabinet.RouteSignUpConfirmation.Replace(":token", token));

				// email confirmation
				await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.EmailConfirmation, userLocale))
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
		[AnonymousAccess]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var audience = JwtAudience.Cabinet;
			var user = (DAL.Models.Identity.User)null;
			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			// check token
			if (! await Core.Tokens.JWT.IsValid(
				appConfig: AppConfig, 
				jwtToken: model.Token, 
				expectedAudience: audience,
				expectedArea: Common.JwtArea.Registration,
				validStamp: async (jwt, id) => {
					user = await UserManager.FindByNameAsync(id);
					return user?.JWTSalt;
				}
			) || user == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			user.EmailConfirmed = true;
			user.JWTSalt = Core.UserAccount.GenerateJwtSalt();
			await DbContext.SaveChangesAsync();

			// load user's options
			await DbContext.Entry(user).Reference(_ => _.UserOptions).LoadAsync();

			// send dpa
			await Core.UserAccount.ResendUserDpaDocument(
				locale: userLocale,
				services: HttpContext.RequestServices,
				user: user,
				email: user.Email,
				redirectUrl: this.MakeAppLink(audience, fragment: AppConfig.Apps.Cabinet.RouteDpaSigned)
			);

			return APIResponse.Success();
		}
	}
}
