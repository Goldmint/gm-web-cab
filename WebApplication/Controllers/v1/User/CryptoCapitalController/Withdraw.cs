using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CryptoCapitalModels;
using Microsoft.AspNetCore.Mvc;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class CryptoCapitalController : BaseController {

		/// <summary>
		/// Withdraw request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("withdraw")]
		[ProducesResponseType(typeof(WithdrawView), 200)]
		public async Task<APIResponse> Withdraw([FromBody] WithdrawModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}
			
			var transCurrency = FiatCurrency.USD;
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			if (amountCents < AppConfig.Constants.CryptoCapitalData.WithdrawMin || (amountCents > AppConfig.Constants.CryptoCapitalData.WithdrawMax && AppConfig.Constants.CryptoCapitalData.WithdrawMax != 0)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			if (!user.TwoFactorEnabled) {
				return APIResponse.BadRequest(APIErrorCode.AccountTFADisabled);
			}

			if (!Core.Tokens.GoogleAuthenticator.Validate(model.Code, user.TFASecret)) {
				return APIResponse.BadRequest(nameof(model.Code), "Invalid code");
			}

			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			// history

			// logic

			var reqId = "?";
			var ccAccountNumber = model.AccountId;

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.CryptoCapital,
				comment: $"Withdraw request #{reqId} to CryptoCapital account #{ccAccountNumber} ({TextFormatter.FormatAmount(amountCents, transCurrency)})",
				ip: agent.Ip,
				agent: agent.Agent
			);

			// ---

			return APIResponse.Success(
				new WithdrawView() {
				}
			);
		}
	}
}
