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
using Goldmint.WebApplication.Core;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

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
			}

			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			var user = await UserManager.FindByNameAsync(model.Username) ?? await UserManager.FindByEmailAsync(model.Username);
			if (user != null) {

				bool isLockedOut = false;

				// locked out
				if (user.LockoutEnd != null && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow) {
					//if (!await Core.Recaptcha.Verify(AppConfig.Services.Recaptcha.SecretKey, model.Captcha, agent.Ip)) {
					//	return APIResponse.BadRequest(APIErrorCode.AccountLocked, "Too many unsuccessful attempts. Account is locked, try to sign in later");
					//}

					// unlock before this check
					isLockedOut = true;
					user.LockoutEnd = null;
				}

				// get audience
				JwtAudience audience = JwtAudience.Cabinet;
				if (!string.IsNullOrWhiteSpace(model.Audience)) {
					if (Enum.TryParse(model.Audience, true, out JwtAudience aud)) {
						audience = aud;
					}
				}

				// load options / dpa doc
				await DbContext.Entry(user).Reference(_ => _.UserOptions).LoadAsync();
				if (user.UserOptions != null) {
					await DbContext.Entry(user.UserOptions).Reference(_ => _.DpaDocument).LoadAsync();

					// check if dpa is signed but email is not confirmed
					if (!user.EmailConfirmed && (user.UserOptions.DpaDocument?.IsSigned ?? false)) {
						user.EmailConfirmed = true;
						await DbContext.SaveChangesAsync();
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
						await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.SignedIn, userLocale))
							.ReplaceBodyTag("IP", agent.Ip)
							.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
							.Send(user.Email, user.UserName, EmailQueue)
						;

						// activity
						var userActivity = CoreLogic.User.CreateUserActivity(
							user: user,
							type: Common.UserActivityType.Auth,
							comment: "Signed in with password",
							ip: agent.Ip,
							agent: agent.Agent,
							locale: userLocale
						);
						DbContext.UserActivity.Add(userActivity);
						await DbContext.SaveChangesAsync();
					}

					return sres;
				}

				// was locked before
				if (isLockedOut) {
					await UserManager.SetLockoutEndDateAsync(user, (DateTimeOffset) DateTime.UtcNow.AddMinutes(60));
					return APIResponse.BadRequest(APIErrorCode.AccountLocked, "Too many unsuccessful attempts. Account is locked, try to sign in later");
				}
			}
				
			return APIResponse.BadRequest(APIErrorCode.AccountNotFound, notFoundDesc);
		}

		/// <summary>
		/// Complete two factor auth
		/// </summary>
		[RequireJWTArea(JwtArea.Tfa)]
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
				if (GoogleAuthenticator.Validate(model.Code, user.TfaSecret)) {
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
			var audience = GetCurrentAudience();

			// new jwt salt
			if (audience != null) {
				Core.UserAccount.GenerateJwtSalt(user, audience.Value);
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success();
		}


		/// <summary>
		/// DPA check
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("dpaCheck")]
		[ProducesResponseType(typeof(AuthenticateView), 200)]
		public async Task<APIResponse> DpaCheck([FromBody] DpaCheckModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var audience = JwtAudience.Cabinet;
			var user = (DAL.Models.Identity.User)null;
			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			// check token
			if (!await Core.Tokens.JWT.IsValid(
				    appConfig: AppConfig,
				    jwtToken: model.Token,
				    expectedAudience: audience,
				    expectedArea: Common.JwtArea.Dpa,
				    validStamp: async (jwt, id) => {
					    user = await UserManager.FindByNameAsync(id);
						return UserAccount.CurrentJwtSalt(user, audience);
				    }
			    ) || user == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			// load options
			await DbContext.Entry(user).Reference(_ => _.UserOptions).LoadAsync();
			if (user.UserOptions?.DpaDocumentId == null) {
				return APIResponse.BadRequest(nameof(model.Token), "Invalid token");
			}

			// load existing dpa doc
			await DbContext.Entry(user.UserOptions).Reference(_ => _.DpaDocument).LoadAsync();
			if (user.UserOptions.DpaDocument.IsSigned) {

				user.EmailConfirmed = true;
				UserAccount.GenerateJwtSalt(user, audience);
				await DbContext.SaveChangesAsync();

				return OnSignInResultCheck(
					services: HttpContext.RequestServices,
					result: SignInResult.Success,
					user: user,
					audience: audience,
					tfaRequired: false
				);
			}

			return APIResponse.BadRequest(APIErrorCode.AccountDpaNotSigned, "DPA is not signed yet");
		}

/*

		/// <summary>
		/// DPA check
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("debugAuth")]
		[ProducesResponseType(typeof(AuthenticateView), 200)]
		public async Task<APIResponse> DebugAuth(long id, string audi) {

			var user = await UserManager.FindByIdAsync(id.ToString());

			JwtAudience audience = JwtAudience.Cabinet;
			if (!string.IsNullOrWhiteSpace(audi)) {
				if (Enum.TryParse(audi, true, out JwtAudience aud)) {
					audience = aud;
				}
			}

			// denied
			var accessRightsMask = Core.UserAccount.ResolveAccessRightsMask(HttpContext.RequestServices, audience, user);
			if (accessRightsMask == null) return null;

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
			});
		}
*/

		// ---

		[NonAction]
		private APIResponse OnSignInResultCheck(IServiceProvider services, Microsoft.AspNetCore.Identity.SignInResult result, DAL.Models.Identity.User user, JwtAudience audience, bool tfaRequired) {
			if (result != null) {

				if (result.Succeeded || result.RequiresTwoFactor) {

					// denied
					var accessRightsMask = Core.UserAccount.ResolveAccessRightsMask(services, audience, user);
					if (accessRightsMask == null) return null;

					// DPA is unsigned
					if (!CoreLogic.User.HasSignedDpa(user.UserOptions)) {
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
									area: JwtArea.Tfa,
									rightsMask: accessRightsMask.Value
								),
								TfaRequired = true,
							}
						);
					}

					// new jwt salt
					UserAccount.GenerateJwtSalt(user, audience);
					DbContext.SaveChanges();

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