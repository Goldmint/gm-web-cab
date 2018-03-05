using Goldmint.Common;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Core.Tokens;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.UserModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/auth")]
	public class AuthController : BaseController {

		/// <summary>
		/// Sign in with username/email and password
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("authenticate")]
		[ProducesResponseType(typeof(AuthenticateView), 200)]
		public async Task<APIResponse> Authenticate([FromBody] AuthenticateModel model) {

			var notFoundDesc = "Account not found";

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotFound, notFoundDesc);
				//return APIResponse.BadRequest(errFields);
			}

			var agent = GetUserAgentInfo();

			// captcha
			if (!HostingEnvironment.IsDevelopment()) {
				if (!await Core.Recaptcha.Verify(AppConfig.Services.Recaptcha.SecretKey, model.Captcha, agent.Ip)) {
					return APIResponse.BadRequest(APIErrorCode.AccountNotFound, notFoundDesc);
				}
			}

			var user = await UserManager.FindByNameAsync(model.Username) ?? await UserManager.FindByEmailAsync(model.Username);
			if (user != null) { // || !await SignInManager.CanSignInAsync(user)) {

				// get audience
				JwtAudience audience = JwtAudience.App;
				if (!string.IsNullOrWhiteSpace(model.Audience)) {
					if (Enum.TryParse(model.Audience, true, out JwtAudience aud)) {
						audience = aud;
					}
				}

				// dpa has not been sent previously
				await DbContext.Entry(user).Reference(_ => _.UserOptions).LoadAsync();
				if (user.UserOptions != null) {
					await DbContext.Entry(user.UserOptions).Reference(_ => _.DPADocument).LoadAsync();
					if (user.UserOptions.DPADocument == null) {
						await Core.UserAccount.ResendUserDpaDocument(
							services: HttpContext.RequestServices,
							user: user,
							email: user.Email,
							redirectUrl: this.MakeLink(fragment: AppConfig.AppRoutes.DpaSigned)
						);
					}
				}

				var sres = OnSignInResultCheck(
					services: HttpContext.RequestServices,
					result: await SignInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true),
					audience: audience,
					user: user,
					tfaRequired: user.TwoFactorEnabled
				);
				if (sres != null) {

					// successful result
					if (sres.GetHttpStatusCode() == System.Net.HttpStatusCode.OK && sres.GetErrorCode() == null) {
						
						// notification
						await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.SignedIn))
							.ReplaceBodyTag("IP", agent.Ip)
							.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
							.Send(user.Email, user.UserName, EmailQueue)
						;

						// activity
						await CoreLogic.UserAccount.SaveActivity(
							services: HttpContext.RequestServices,
							user: user,
							type: Common.UserActivityType.Auth,
							comment: "Signed in with password",
							ip: agent.Ip,
							agent: agent.Agent
						);
					}

					return sres;
				}
			}
				
			return APIResponse.BadRequest(APIErrorCode.AccountNotFound, notFoundDesc);
		}

		/// <summary>
		/// Complete two factor auth
		/// </summary>
		[RequireJWTArea(JwtArea.TFA)]
		[HttpPost, Route("tfa")]
		[ProducesResponseType(typeof(AuthenticateView), 200)]
		public async Task<APIResponse> Tfa([FromBody] TfaModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var audience = GetCurrentAudience();
			if (audience == null) {
				return APIResponse.BadRequest(APIErrorCode.Unauthorized);
			}

			var user = await GetUserFromDb();
			if (user != null && user.TwoFactorEnabled) {

				// locked out
				if (await UserManager.IsLockedOutAsync(user)) {
					return APIResponse.BadRequest(APIErrorCode.AccountLocked, "Too many unsuccessful attempts. Account is locked, try to sign in later");
				}

				// by code
				if (GoogleAuthenticator.Validate(model.Code, user.TFASecret)) {
					return OnSignInResultCheck(
						services: HttpContext.RequestServices,
						result: Microsoft.AspNetCore.Identity.SignInResult.Success, 
						user: user, 
						audience: audience.Value,
						tfaRequired: false
					);
				}

				// +1 failed login
				DbContext.Attach<DAL.Models.Identity.User>(user);
				await UserManager.AccessFailedAsync(user);
			}

			return APIResponse.BadRequest(nameof(model.Code), "Invalid code");
		}

		/// <summary>
		/// Access token exchange
		/// </summary>
		[RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("refresh")]
		[ProducesResponseType(typeof(RefreshView), 200)]
		public async Task<APIResponse> Refresh() {

			var user = await GetUserFromDb();

			var audience = GetCurrentAudience();
			if (audience == null) {
				return APIResponse.BadRequest(APIErrorCode.Unauthorized);
			}

			return APIResponse.Success(
				new RefreshView() {
					Token = JWT.CreateAuthToken(
						appConfig: AppConfig, 
						user: user, 
						audience: audience.Value,
						area: JwtArea.Authorized,
						rightsMask: GetCurrentRights()
					),
				}
			);
		}

		/// <summary>
		/// Sign out
		/// </summary>
		[RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("signout")]
		public async Task<APIResponse> SignOut() {
			var user = await GetUserFromDb();
			return APIResponse.Success();
		}

		// ---

		[NonAction]
		private APIResponse OnSignInResultCheck(IServiceProvider services, Microsoft.AspNetCore.Identity.SignInResult result, DAL.Models.Identity.User user, JwtAudience audience, bool tfaRequired) {
			if (result != null) {

				if (result.Succeeded || result.RequiresTwoFactor) {

					// denied
					var accessRightsMask = Core.UserAccount.ResolveAccessRightsMask(services, audience, user);
					if (accessRightsMask == null) return null;

					// DPA is unsigned
					if (!CoreLogic.UserAccount.HasSignedDpa(user)) {
						return APIResponse.BadRequest(APIErrorCode.AccountDpaNotSigned, "DPA is not signed yet");
					}

					// tfa token
					if (tfaRequired || result.RequiresTwoFactor) {
						return APIResponse.Success(
							new AuthenticateView() {
								Token = JWT.CreateAuthToken(
									appConfig: AppConfig, 
									user: user, 
									audience: audience,
									area: JwtArea.TFA,
									rightsMask: accessRightsMask.Value
								),
								TfaRequired = true,
							}
						);
					}

					// auth token
					return APIResponse.Success(
						new AuthenticateView() {
							Token = JWT.CreateAuthToken(
								appConfig: AppConfig, 
								user: user, 
								audience: audience,
								area: JwtArea.Authorized,
								rightsMask: accessRightsMask.Value
							),
							TfaRequired = false,
						}
					);
				}

				if (result.IsLockedOut) {
					return APIResponse.BadRequest(APIErrorCode.AccountLocked, "Too many unsuccessful attempts to sign in. Account is locked, try to sign in later");
				}

				if (result.IsNotAllowed) {
					return APIResponse.BadRequest(APIErrorCode.AccountEmailNotConfirmed, "Email is not confirmed yet");
				}
			}

			// not found
			return null;
		}
	}
}