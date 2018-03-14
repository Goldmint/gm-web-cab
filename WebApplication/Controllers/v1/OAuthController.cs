using Goldmint.Common;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Core.Tokens;
using Goldmint.WebApplication.Models.API.v1.OAuthModels;
using Goldmint.WebApplication.Services.OAuth;
using Goldmint.WebApplication.Services.OAuth.Impl;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/oauth")]
	public class OAuthController : BaseController {

		/// <summary>
		/// Create redirect
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("google")]
		[ProducesResponseType(typeof(RedirectView), 200)]
		public async Task<APIResponse> Google() {

			var provider = HttpContext.RequestServices.GetRequiredService<GoogleProvider>();

			return APIResponse.Success(
				new RedirectView() {
					Redirect = await provider.GetRedirect(
						Url.Link("OAuthGoogleCallback", new { }),
						null
					),
				}
			);
		}

		/// <summary>
		/// On callback
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("googleCallback", Name = "OAuthGoogleCallback")]
		[ProducesResponseType(302)]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> GoogleCallback(string error, string code, string state) {
			try {

				if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) {
					throw new Exception("Invalid callback result");
				}

				var provider = HttpContext.RequestServices.GetRequiredService<GoogleProvider>();
				var userInfo = await provider.GetUserInfo(
					Url.Link("OAuthGoogleCallback", new { }),
					state,
					code
				);

				return await ProcessOAuthCallback(
					LoginProvider.Google,
					userInfo
				);
			}
			catch {
				return Redirect("/");
			}
		}

		// ---

		[NonAction]
		public async Task<RedirectResult> ProcessOAuthCallback(LoginProvider provider, UserInfo userInfo) {

			var audience = JwtAudience.App;

			// find user with this ext login
			var user = await UserManager.FindByLoginAsync(provider.ToString(), userInfo.Id);
			var userLocale = Locale.En;

			// exists
			if (user != null) {

				var agent = GetUserAgentInfo();

				// try to sign in
				var signResult = await SignInManager.CanSignInAsync(user);

				var accessRightsMask = Core.UserAccount.ResolveAccessRightsMask(HttpContext.RequestServices, audience, user);

				if (signResult && accessRightsMask != null) {

					// get options for DPA check
					await DbContext.Entry(user).Reference(_ => _.UserOptions).LoadAsync();
					if (user.UserOptions != null) {
						await DbContext.Entry(user.UserOptions).Reference(_ => _.DPADocument).LoadAsync();
					}

					// DPA is unsigned
					if (user.UserOptions != null && !CoreLogic.User.HasSignedDpa(user.UserOptions)) {
						
						// has not been sent previously
						if (user.UserOptions.DPADocument == null) {
							await Core.UserAccount.ResendUserDpaDocument(
								locale: userLocale,
								services: HttpContext.RequestServices,
								user: user,
								email: user.Email,
								redirectUrl: this.MakeLink(fragment: AppConfig.AppRoutes.DpaSigned)
							);
						}

						return Redirect(
							this.MakeLink(fragment: AppConfig.AppRoutes.DpaRequired)
						);
					}

					// notification
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.SignedIn, userLocale))
						.ReplaceBodyTag("IP", agent.Ip)
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, user.UserName, EmailQueue)
					;

					// activity
					await CoreLogic.User.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.Auth,
						comment: "Signed in with social network",
						ip: agent.Ip,
						agent: agent.Agent
					);

					// tfa required
					if (user.TwoFactorEnabled) {
						var tokenForTFA = JWT.CreateAuthToken(
							appConfig: AppConfig,
							user: user,
							audience: JwtAudience.App,
							area: JwtArea.TFA,
							rightsMask: accessRightsMask.Value
						);

						return Redirect(
							this.MakeLink(fragment: AppConfig.AppRoutes.OAuthTfaPage.Replace(":token", tokenForTFA))
						);
					}

					// new jwt salt
					user.JWTSalt = Core.UserAccount.GenerateJwtSalt();
					DbContext.SaveChanges();

					// ok
					var token = JWT.CreateAuthToken(
						appConfig: AppConfig, 
						user: user, 
						audience: JwtAudience.App,
						area: JwtArea.Authorized,
						rightsMask: accessRightsMask.Value
					);
					return Redirect(
						this.MakeLink(fragment: AppConfig.AppRoutes.OAuthAuthorized.Replace(":token", token))
					);
				}

				// never should get here
				return Redirect("/");
			}

			// doesnt exists yet
			else {

				var email = userInfo.Email;

				// try create and sign in
				var cuaResult = await Core.UserAccount.CreateUserAccount(HttpContext.RequestServices, email, emailConfirmed: true);
				if (cuaResult.User != null) {

					// user created and external login attached
					if (await CreateExternalLogin(cuaResult.User, provider, userInfo)) {

						var accessRightsMask = Core.UserAccount.ResolveAccessRightsMask(HttpContext.RequestServices, audience, cuaResult.User);
						if (accessRightsMask != null) {

							// send dpa
							await Core.UserAccount.ResendUserDpaDocument(
								locale: userLocale,
								services: HttpContext.RequestServices,
								user: cuaResult.User,
								email: cuaResult.User.Email,
								redirectUrl: this.MakeLink(fragment: AppConfig.AppRoutes.DpaSigned)
							);

							// DPA is unsigned
							if (!CoreLogic.User.HasSignedDpa(cuaResult.User.UserOptions)) {
								return Redirect(
									this.MakeLink(fragment: AppConfig.AppRoutes.DpaRequired)
								);
							}

							// ok
							var token = JWT.CreateAuthToken(
								appConfig: AppConfig,
								user: cuaResult.User,
								audience: JwtAudience.App,
								area: JwtArea.Authorized,
								rightsMask: accessRightsMask.Value
							);
							return Redirect(
								this.MakeLink(fragment: AppConfig.AppRoutes.OAuthAuthorized.Replace(":token", token))
							);
						}
					}

					// failed
					return Redirect("/");
				}

				// redirect to error OR email input
				return Redirect(
					this.MakeLink(fragment: AppConfig.AppRoutes.EmailTaken)
				);
			}
		}

		[NonAction]
		private async Task<bool> CreateExternalLogin(DAL.Models.Identity.User user, LoginProvider provider, UserInfo userInfo) {

			// attach login
			var res = await UserManager.AddLoginAsync(user, new UserLoginInfo(provider.ToString(), userInfo.Id, provider.ToString()));

			// sign in
			if (res.Succeeded && await SignInManager.CanSignInAsync(user)) {
				return true;
			}
			return false;
		}

	}
}
