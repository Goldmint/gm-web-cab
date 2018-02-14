using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.SettingsModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	public partial class SettingsController : BaseController {

		/// <summary>
		/// Get TFA status
		/// </summary>
		[AreaAuthorized]
		[HttpGet, Route("tfa/view")]
		[ProducesResponseType(typeof(TFAView), 200)]
		public async Task<APIResponse> TFAView() {
			var user = await GetUserFromDb();
			return APIResponse.Success(MakeTFASetupView(user));
		}

		/// <summary>
		/// Set TFA status
		/// </summary>
		[AreaAuthorized]
		[HttpPost, Route("tfa/edit")]
		[ProducesResponseType(typeof(TFAView), 200)]
		public async Task<APIResponse> TFAEdit([FromBody] TFAEditModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// check
			if (!Core.Tokens.GoogleAuthenticator.Validate(model.Code, user.TFASecret)) {
				return APIResponse.BadRequest(nameof(model.Code), "Invalid 2fa code");
			}

			var sendNoti = user.TwoFactorEnabled != model.Enable;

			user.TwoFactorEnabled = model.Enable;
			user.UserOptions.InitialTFAQuest = true;

			//DbContext.Update(user.UserOptions);
			DbContext.Update(user);
			await DbContext.SaveChangesAsync();

			// notify
			if (sendNoti) {
				if (model.Enable) {
					
					// notification
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.TfaEnabled))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, EmailQueue)
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
					await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.TfaDisabled))
						.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
						.Send(user.Email, EmailQueue)
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
		private TFAView MakeTFASetupView(User user) {

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
