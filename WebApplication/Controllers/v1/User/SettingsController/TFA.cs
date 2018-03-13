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
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("tfa/view")]
		[ProducesResponseType(typeof(TFAView), 200)]
		public async Task<APIResponse> TFAView() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeTFASetupView(user));
		}

		/// <summary>
		/// Set TFA status
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("tfa/edit")]
		[ProducesResponseType(typeof(TFAView), 200)]
		public async Task<APIResponse> TFAEdit([FromBody] TFAEditModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();
			var userLocale = Locale.En;

			var makeChange = user.TwoFactorEnabled != model.Enable;

			if (makeChange) {
				if (!Core.Tokens.GoogleAuthenticator.Validate(model.Code, user.TFASecret)) {
					return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
				}
				user.TwoFactorEnabled = model.Enable;
			}

			user.UserOptions.InitialTFAQuest = true;
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
					await CoreLogic.UserAccount.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.Settings,
						comment: "Two factor authentication enabled",
						ip: agent.Ip,
						agent: agent.Agent
					);
				}
				else {

					// notification
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.TfaDisabled, userLocale))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, user.UserName, EmailQueue)
					;

					// activity
					await CoreLogic.UserAccount.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.Settings,
						comment: "Two factor authentication disabled",
						ip: agent.Ip,
						agent: agent.Agent
					);
				}
			}

			return APIResponse.Success(MakeTFASetupView(user));
		}

		// ---

		[NonAction]
		private TFAView MakeTFASetupView(DAL.Models.Identity.User user) {

			var ret = new TFAView() {
				Enabled = user.TwoFactorEnabled,
				QrData = null,
				Secret = null,
			};

			if (!user.TwoFactorEnabled) {

				var secretBytes = System.Text.Encoding.ASCII.GetBytes(user.TFASecret);
				var secretBase32 = Wiry.Base32.Base32Encoding.Standard.GetString(secretBytes).Replace("=", "").ToUpper();

				ret.QrData = Core.Tokens.GoogleAuthenticator.MakeQRCode("goldmint.io", user.UserName, secretBase32);
				ret.Secret = secretBase32;
			}

			return ret;
		}
	}
}
