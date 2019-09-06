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
		/// Get TFA status
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("tfa/view")]
		[ProducesResponseType(typeof(TfaView), 200)]
		public async Task<APIResponse> TFAView() {
			var user = await GetUserFromDb();

			// randomize tfa secret
			if (!user.TwoFactorEnabled) {
				user.TfaSecret = Core.UserAccount.GenerateTfaSecret();
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(MakeTFASetupView(user));
		}

		/// <summary>
		/// Set TFA status
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpPost, Route("tfa/edit")]
		[ProducesResponseType(typeof(TfaView), 200)]
		public async Task<APIResponse> TFAEdit([FromBody] TfaEditModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();
			var userLocale = GetUserLocale();

			var makeChange = user.TwoFactorEnabled != model.Enable;

			if (makeChange) {
				if (!Core.Tokens.GoogleAuthenticator.Validate(model.Code, user.TfaSecret)) {
					return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
				}
				user.TwoFactorEnabled = model.Enable;
			}

			user.UserOptions.InitialTfaQuest = true;
			await DbContext.SaveChangesAsync();

			// notify
			if (makeChange) {
				if (model.Enable) {
					
					// notification
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.TfaEnabled, userLocale))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, user.UserName, EmailQueue)
					;

					// activity
					var userActivity = CoreLogic.User.CreateUserActivity(
						user: user,
						type: Common.UserActivityType.Settings,
						comment: "Two factor authentication enabled",
						ip: agent.Ip,
						agent: agent.Agent,
						locale: userLocale
					);
					DbContext.UserActivity.Add(userActivity);
					await DbContext.SaveChangesAsync();
				}
				else {

					// notification
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.TfaDisabled, userLocale))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, user.UserName, EmailQueue)
					;

					// activity
					var userActivity = CoreLogic.User.CreateUserActivity(
						user: user,
						type: Common.UserActivityType.Settings,
						comment: "Two factor authentication disabled",
						ip: agent.Ip,
						agent: agent.Agent,
						locale: userLocale
					);
					DbContext.UserActivity.Add(userActivity);
					await DbContext.SaveChangesAsync();
				}
			}

			return APIResponse.Success(MakeTFASetupView(user));
		}

		// ---

		[NonAction]
		private TfaView MakeTFASetupView(DAL.Models.Identity.User user) {

			var ret = new TfaView() {
				Enabled = user.TwoFactorEnabled,
				QrData = null,
				Secret = null,
			};

			if (!user.TwoFactorEnabled) {

				var secretBytes = System.Text.Encoding.ASCII.GetBytes(user.TfaSecret);
				var secretBase32 = Wiry.Base32.Base32Encoding.Standard.GetString(secretBytes).Replace("=", "").ToUpper();

				ret.QrData = Core.Tokens.GoogleAuthenticator.MakeQRCode(AppConfig.Auth.TwoFactorIssuer, user.UserName, secretBase32);
				ret.Secret = secretBase32;
			}

			return ret;
		}
	}
}
