using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.ExchangeModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class ExchangeController : BaseController {

		/// <summary>
		/// Buying request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/buy")]
		[ProducesResponseType(typeof(BuyRequestView), 200)]
		public async Task<APIResponse> BuyRequest([FromBody] BuyRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			// check pending operations
			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var currency = FiatCurrency.USD;
			var mntpBalance = model.EthAddress == null ? BigInteger.Zero : await EthereumObserver.GetAddressMntpBalance(model.EthAddress);
			var fiatBalance = await EthereumObserver.GetUserFiatBalance(user.UserName, currency);
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateBuying(
				fiatAmountCents: amountCents,
				fiatTotalVolumeCents: fiatBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountCents != estimated.InputUsed) {
				return APIResponse.BadRequest(nameof(model.Amount), "Amount is invalid");
			}

			var expiresIn = TimeSpan.FromSeconds(AppConfig.Constants.TimeLimits.BuySellRequestExpireSec);
			var ticket = await TicketDesk.NewGoldBuying(user, model.EthAddress, currency, amountCents, goldRate, mntpBalance, estimated.ResultGold, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.GoldBuy,
				AmountCents = estimated.InputUsed,
				FeeCents = estimated.ResultFeeCents,
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
			var buyRequest = new BuyRequest() {
				Status = GoldExchangeRequestStatus.Unconfirmed,
				Type = GoldExchangeRequestType.EthRequest,
				Currency = currency,
				FiatAmountCents = estimated.InputUsed,
				Address = model.EthAddress,
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,

				RefFinancialHistoryId = finHistory.Id,
				UserId = user.Id,
			};

			// add and save
			DbContext.BuyRequest.Add(buyRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Buying order #{buyRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.ResultGold)} GOLD";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD buying request #{buyRequest.Id} ({TextFormatter.FormatAmount(amountCents, currency)}) from {Common.TextFormatter.MaskBlockchainAddress(model.EthAddress)} initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new BuyRequestView() {
					GoldAmount = estimated.ResultGold.ToString(),
					GoldRate = goldRate / 100d,
					Payload = new[] { user.UserName, buyRequest.Id.ToString() },
					RequestId = buyRequest.Id,
					ExpiresIn = (long)expiresIn.TotalSeconds,
				}
			);
		}

		/// <summary>
		/// Buying request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/buy")]
		[ProducesResponseType(typeof(HWBuyRequestView), 200)]
		public async Task<APIResponse> HwBuyRequest([FromBody] HWBuyRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			// check pending operations
			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var currency = FiatCurrency.USD;
			var mntpBalance = BigInteger.Zero;
			var fiatBalance = await EthereumObserver.GetUserFiatBalance(user.UserName, currency);
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateBuying(
				fiatAmountCents: amountCents,
				fiatTotalVolumeCents: fiatBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountCents != estimated.InputUsed) {
				return APIResponse.BadRequest(nameof(model.Amount), "Amount is invalid");
			}

			// check rate
			var opLastTime = user.UserOptions.HotWalletBuyingLastTime;
			if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
				// failed
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			var ticket = await TicketDesk.NewGoldBuying(user, null, currency, amountCents, goldRate, mntpBalance, estimated.ResultGold, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.GoldBuy,
				AmountCents = estimated.InputUsed,
				FeeCents = estimated.ResultFeeCents,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var buyRequest = new BuyRequest() {
				Status = GoldExchangeRequestStatus.Unconfirmed,
				Type = GoldExchangeRequestType.HWRequest,
				Currency = currency,
				FiatAmountCents = estimated.InputUsed,
				Address = "HW",
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,

				RefFinancialHistoryId = finHistory.Id,
				UserId = user.Id,
			};

			// add and save
			DbContext.BuyRequest.Add(buyRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Buying order #{buyRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.ResultGold)} GOLD";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD buying request #{buyRequest.Id} ({TextFormatter.FormatAmount(amountCents, currency)}) to hot wallet initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new HWBuyRequestView() {
					GoldAmount = estimated.ResultGold.ToString(),
					GoldRate = goldRate / 100d,
					RequestId = buyRequest.Id,
				}
			);
		}

	}
}