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

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class ExchangeController : BaseController {

		/// <summary>
		/// Buying request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
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

			var ticket = await TicketDesk.NewGoldBuying(user, model.EthAddress, currency, amountCents, goldRate, mntpBalance, estimated.ResultGold, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.GoldBuy,
				AmountCents = estimated.InputUsed,
				FeeCents = estimated.ResultFeeCents,
				Currency = currency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var buyRequest = new BuyRequest() {
				User = user,
				Type = ExchangeRequestType.EthRequest,
				Status = ExchangeRequestStatus.Initial,
				Currency = currency,
				FiatAmountCents = estimated.InputUsed,
				Address = model.EthAddress,
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,
				RefFinancialHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyRequest.Add(buyRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Buying order #{buyRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.ResultGold)} GOLD";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD buying request #{buyRequest.Id} ({TextFormatter.FormatAmount(amountCents, currency)}) from {Common.TextFormatter.MaskEthereumAddress(model.EthAddress)} initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new BuyRequestView() {
					GoldAmount = estimated.ResultGold.ToString(),
					GoldRate = goldRate / 100d,
					Payload = new[] { user.UserName, buyRequest.Id.ToString() },
				}
			);
		}

		/// <summary>
		/// Buying request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/buy")]
		[ProducesResponseType(typeof(HWBuyRequestView), 200)]
		public async Task<APIResponse> HWBuyRequest([FromBody] HWBuyRequestModel model) {

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
				return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
			}

			var ticket = await TicketDesk.NewGoldBuying(user, null, currency, amountCents, goldRate, mntpBalance, estimated.ResultGold, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.GoldBuy,
				AmountCents = estimated.InputUsed,
				FeeCents = estimated.ResultFeeCents,
				Currency = currency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var buyRequest = new BuyRequest() {
				User = user,
				Type = ExchangeRequestType.HWRequest,
				Status = ExchangeRequestStatus.Initial,
				Currency = currency,
				FiatAmountCents = estimated.InputUsed,
				Address = "HW",
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,
				RefFinancialHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyRequest.Add(buyRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Buying order #{buyRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.ResultGold)} GOLD";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.UserAccount.SaveActivity(
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