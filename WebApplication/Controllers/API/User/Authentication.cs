using Goldmint.Common;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Core.Tokens;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.UserModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	[Route("api/v1/user")]
	public partial class UserController : BaseController {

		/// <summary>
		/// Sign in with username/email and password
		/// </summary>
		[AreaAnonymous]
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

				var sres = OnSignInResult(
					await SignInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true),
					user,
					tfaRequired: user.TwoFactorEnabled
				);
				if (sres != null) {

					// successful result
					if (sres.GetHttpStatusCode() == System.Net.HttpStatusCode.OK && sres.GetErrorCode() == null) {
						
						// notification
						await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.SignedIn))
							.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
							.Send(user.Email, EmailQueue)
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
		[AreaTFA]
		[HttpPost, Route("tfa")]
		[ProducesResponseType(typeof(AuthenticateView), 200)]
		public async Task<APIResponse> Tfa([FromBody] TfaModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			if (user != null && user.TwoFactorEnabled) {

				// locked out
				if (await UserManager.IsLockedOutAsync(user)) {
					return APIResponse.BadRequest(APIErrorCode.AccountLocked, "Too many unsuccessful attempts. Account is locked, try to sign in later");
				}

				// by code
				if (GoogleAuthenticator.Validate(model.Code, user.TFASecret)) {
					return OnSignInResult(Microsoft.AspNetCore.Identity.SignInResult.Success, user, false);
				}

				// +1 failed login
				DbContext.Attach(user);
				await UserManager.AccessFailedAsync(user);
			}

			return APIResponse.BadRequest(nameof(model.Code), "Invalid code");
		}

		/// <summary>
		/// Access token exchange
		/// </summary>
		[AreaAuthorized]
		[HttpGet, Route("refresh")]
		[ProducesResponseType(typeof(RefreshView), 200)]
		public async Task<APIResponse> Refresh() {

			var user = await GetUserFromDb();

			user.AccessStamp = Core.UserAccount.GenerateAccessStamp();
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new RefreshView() {
					Token = JWT.CreateAuthToken(AppConfig, user, JwtArea.Authorized),
				}
			);
		}

		/// <summary>
		/// Invalidate current access token
		/// </summary>
		[AreaAuthorized]
		[HttpGet, Route("signout")]
		public async Task<APIResponse> SignOut() {
			var user = await GetUserFromDb();

			user.AccessStamp = Core.UserAccount.GenerateAccessStamp();
			await DbContext.SaveChangesAsync();

			return APIResponse.Success();
		}

		// ---

		[NonAction]
		private async Task<bool> CreateExternalLogin(User user, ExternalLoginInfo info) {
			var res = await UserManager.AddLoginAsync(user, info);
			if (res.Succeeded && await SignInManager.CanSignInAsync(user)) {
				await SignInManager.SignInAsync(user, isPersistent: false);
				return true;
			}
			return false;
		}

		[NonAction]
		private APIResponse OnSignInResult(Microsoft.AspNetCore.Identity.SignInResult result, User user, bool tfaRequired) {
			if (result != null) {

				if (result.Succeeded || result.RequiresTwoFactor) {

					// tfa token
					if (tfaRequired || result.RequiresTwoFactor) {
						return APIResponse.Success(
							new AuthenticateView() {
								Token = JWT.CreateAuthToken(AppConfig, user, JwtArea.TFA),
								TfaRequired = true,
							}
						);
					}

					// auth token
					return APIResponse.Success(
						new AuthenticateView() {
							Token = JWT.CreateAuthToken(AppConfig, user, JwtArea.Authorized),
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