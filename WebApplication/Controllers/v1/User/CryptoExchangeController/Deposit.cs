using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CryptoExchangeModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class CryptoExchangeController : BaseController {

		/// <summary>
		/// Eth deposit request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("depositEth")]
		[ProducesResponseType(typeof(EthDepositView), 200)]
		public async Task<APIResponse> EthDeposit([FromBody] EthDepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var amountWei = BigInteger.Zero;
			if (!BigInteger.TryParse(model.Amount, out amountWei) || amountWei.ToString().Length > 64 || amountWei < 1) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			// check rate
			var opLastTime = user.UserOptions.CryptoDepositLastTime;
			if (opLastTime != null && (DateTime.UtcNow - opLastTime) < CryptoExchangeOperationTimeLimit) {
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			// check pending operations
			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var currency = FiatCurrency.USD;
			var ethRate = await CryptoassetRateProvider.GetRate(CryptoExchangeAsset.ETH, currency);
			ethRate = ethRate - (long)Math.Round(ethRate * AppConfig.Constants.CryptoExchange.DepositFiatConversionBuffer);
			if (ethRate <= 0) {
				throw new Exception("Eth rate is <= 0");
			}

			// ---

			var expiresIn = TimeSpan.FromSeconds(AppConfig.Constants.TimeLimits.CryptoExchangeRequestExpireSec);
			var ticket = await TicketDesk.NewCryptoDeposit(user, CryptoExchangeAsset.ETH, model.EthAddress, currency, ethRate);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.CryptoDeposit,
				AmountCents = 0,
				FeeCents = 0,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeExpires = DateTime.UtcNow.Add(expiresIn),
				UserId = user.Id,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var depositRequest = new CryptoDeposit() {
				Status = CryptoDepositStatus.Unconfirmed,
				Origin = CryptoExchangeAsset.ETH,
				Address = model.EthAddress,
				RequestedAmount = amountWei.ToString(),
				Currency = currency,
				RateCents = ethRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,

				UserId = user.Id,
				RefFinancialHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.CryptoDeposit.Add(depositRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Ethereum deposit request #{depositRequest.Id} at {TextFormatter.FormatAmount(ethRate, currency)} per ETH";
			await DbContext.SaveChangesAsync();
	
			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Cryptoassets,
				comment: $"Ethereum deposit #{depositRequest.Id} requested ({TextFormatter.FormatAmount(ethRate, currency)} per ETH)",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new EthDepositView() {
					EthRate = ethRate / 100d,
					RequestId = depositRequest.Id,
					ExpiresIn = (long)expiresIn.TotalSeconds,
				}
			);
		}

	}
}