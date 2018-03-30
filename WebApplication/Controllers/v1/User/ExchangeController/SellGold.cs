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
		/// Selling request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/sell")]
		[ProducesResponseType(typeof(SellRequestView), 200)]
		public async Task<APIResponse> SellRequest([FromBody] SellRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var currency = FiatCurrency.USD;

			if (!BigInteger.TryParse(model.Amount, out var amountWei)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}
			
			// ---

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var goldBalance = model.EthAddress == null ? BigInteger.Zero : await EthereumObserver.GetAddressGoldBalance(model.EthAddress);
			var mntpBalance = model.EthAddress == null ? BigInteger.Zero : await EthereumObserver.GetAddressMntpBalance(model.EthAddress);
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateSelling(
				goldAmountWei: amountWei,
				goldTotalVolumeWei: goldBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountWei < estimated.InputMin || amountWei > estimated.InputMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var expiresIn = TimeSpan.FromSeconds(AppConfig.Constants.TimeLimits.BuySellRequestExpireSec);
			var ticket = await TicketDesk.NewGoldSelling(user, model.EthAddress, currency, estimated.InputUsed, goldRate, mntpBalance, estimated.ResultNetCents, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.GoldSell,
				AmountCents = estimated.ResultGrossCents,
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
			var sellRequest = new SellRequest() {
				Status = GoldExchangeRequestStatus.Unconfirmed,
				Type = GoldExchangeRequestType.EthRequest,
				Currency = currency,
				FiatAmountCents = estimated.ResultGrossCents,
				Address = model.EthAddress,
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,

				RefFinancialHistoryId = finHistory.Id,
				UserId = user.Id,
			};

			// add and save
			DbContext.SellRequest.Add(sellRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Selling order #{sellRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.InputUsed)} GOLD";
			await DbContext.SaveChangesAsync();
	
			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD selling request #{sellRequest.Id} ({TextFormatter.FormatAmount(estimated.ResultGrossCents, currency)}) from {Common.TextFormatter.MaskBlockchainAddress(model.EthAddress)} initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new SellRequestView() {
					GoldAmount = estimated.InputUsed.ToString(),
					FiatAmount = estimated.ResultNetCents / 100d,
					FeeAmount = estimated.ResultFeeCents / 100d,
					GoldRate = goldRate / 100d,
					Payload = new[] { user.UserName, sellRequest.Id.ToString() },
					RequestId = sellRequest.Id,
					ExpiresIn = (long)expiresIn.TotalSeconds,
				}
			);
		}

		/// <summary>
		/// Selling request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/sell")]
		[ProducesResponseType(typeof(HWSellRequestView), 200)]
		public async Task<APIResponse> HwSellRequest([FromBody] HWSellRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var currency = FiatCurrency.USD;

			if (!BigInteger.TryParse(model.Amount, out var amountWei)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}
			
			// ---

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			var opLastTime = user.UserOptions.HotWalletSellingLastTime;
			if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			}

			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var goldBalance = await EthereumObserver.GetUserGoldBalance(user.UserName);
			var mntpBalance = BigInteger.Zero;
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateSelling(
				goldAmountWei: amountWei,
				goldTotalVolumeWei: goldBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountWei < estimated.InputMin || amountWei > estimated.InputMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var ticket = await TicketDesk.NewGoldSelling(user, null, currency, estimated.InputUsed, goldRate, mntpBalance, estimated.ResultNetCents, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.GoldSell,
				AmountCents = estimated.ResultGrossCents,
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
			var sellRequest = new SellRequest() {
				Status = GoldExchangeRequestStatus.Unconfirmed,
				Type = GoldExchangeRequestType.HWRequest,
				Currency = currency,
				FiatAmountCents = estimated.ResultGrossCents,
				Address = "HW",
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,

				RefFinancialHistoryId = finHistory.Id,
				UserId = user.Id,
			};

			// add and save
			DbContext.SellRequest.Add(sellRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Selling order #{sellRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.InputUsed)} GOLD";
			await DbContext.SaveChangesAsync();
	
			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD selling request #{sellRequest.Id} ({TextFormatter.FormatAmount(estimated.ResultGrossCents, currency)}) from hot wallet initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new HWSellRequestView() {
					GoldAmount = estimated.InputUsed.ToString(),
					FiatAmount = estimated.ResultNetCents / 100d,
					FeeAmount = estimated.ResultFeeCents / 100d,
					GoldRate = goldRate / 100d,
					RequestId = sellRequest.Id,
				}
			);
		}

	}
}