using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Microsoft.AspNetCore.Mvc;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using System.Threading.Tasks;
using System.Linq;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using System;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Globalization;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/gold/buy")]
	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// Estimate
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("estimate")]
		[ProducesResponseType(typeof(EstimateView), 200)]
		public async Task<APIResponse> Estimate([FromBody] EstimateModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}
			
			var exchangeCurrency = FiatCurrency.USD;

			// ---

			// TODO: use GoldToken safe estimation

			FiatCurrency? fiatCurrency = null;
			CryptoCurrency? cryptoCurrency = null;
			var inputRate = 0L;

			// try parse fiat currency
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				fiatCurrency = fc;
				exchangeCurrency = fc;
				inputRate = 100; // 1:1
			}
			// or crypto currency
			else if (Enum.TryParse(model.Currency, true, out CryptoCurrency cc)) {
				cryptoCurrency = cc;
				inputRate = await CryptoassetRateProvider.GetRate(cc, exchangeCurrency);
			}
			else {
				return APIResponse.BadRequest(nameof(model.Currency), "Invalid format");
			}

			// ---

			var inputDecimals = 0;

			if (fiatCurrency != null) {
				inputDecimals = 2;
			}
			else if (cryptoCurrency == CryptoCurrency.ETH) {
				inputDecimals = Tokens.ETH.Decimals;
			}

			// ---

			BigInteger.TryParse(model.Amount, out var inputAmount);
			if (inputAmount <= 0) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var goldRate = await GoldRateCached.GetGoldRate(exchangeCurrency);

			// ---

			var exchangeAmount = inputAmount * new BigInteger(inputRate);
			var goldAmount = exchangeAmount * BigInteger.Pow(10, Tokens.GOLD.Decimals) / goldRate / BigInteger.Pow(10, inputDecimals);

			return APIResponse.Success(
				new EstimateView() {
					Amount = goldAmount.ToString(),
				}
			);
		}

		/// <summary>
		/// Confirm buying/selling request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(ConfirmView), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var requestBuying = await (
				from r in DbContext.BuyGoldRequest
				where
					r.Status == BuyGoldRequestStatus.Unconfirmed &&
					r.Id == model.RequestId &&
					r.UserId == user.Id &&
					r.TimeExpires > DateTime.UtcNow
				select r
			)
			.Include(_ => _.RefUserFinHistory)
			.AsTracking()
			.FirstOrDefaultAsync()
			;
			
			// request not exists
			if (requestBuying == null) {
				return APIResponse.BadRequest(nameof(model.RequestId), "Invalid id");
			}

			// mark request for processing
			requestBuying.RefUserFinHistory.Status = UserFinHistoryStatus.Manual;
			requestBuying.Status = BuyGoldRequestStatus.Confirmed;

			await DbContext.SaveChangesAsync();

			try {
				await TicketDesk.UpdateTicket(requestBuying.DeskTicketId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch {
			}

			return APIResponse.Success(
				new ConfirmView() { }
			);
		}

		/*
				/// <summary>
				/// Buying request
				/// </summary>
				[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
				[HttpPost, Route("buy")]
				[ProducesResponseType(typeof(BuyRequestView), 200)]
				public async Task<APIResponse> BuyRequest([FromBody] BuyRequestModel model) {

					// validate
					if (BaseValidableModel.IsInvalid(model, out var errFields)) {
						return APIResponse.BadRequest(errFields);
					}

					var currency = FiatCurrency.USD;

					var amountCents = (long)Math.Floor(model.Amount * 100d);
					model.Amount = amountCents / 100d;

					// ---

					var user = await GetUserFromDb();
					var agent = GetUserAgentInfo();

					if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
						return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
					}

					// ---

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
				[HttpPost, Route("buy")]
				[ProducesResponseType(typeof(HWBuyRequestView), 200)]
				public async Task<APIResponse> HwBuyRequest([FromBody] HWBuyRequestModel model) {

					// validate
					if (BaseValidableModel.IsInvalid(model, out var errFields)) {
						return APIResponse.BadRequest(errFields);
					}

					var currency = FiatCurrency.USD;

					var amountCents = (long)Math.Floor(model.Amount * 100d);
					model.Amount = amountCents / 100d;

					// ---

					var user = await GetUserFromDb();
					var agent = GetUserAgentInfo();

					var opLastTime = user.UserOptions.HotWalletBuyingLastTime;
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
						return APIResponse.BadRequest(APIErrorCode.RateLimit);
					}

					if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
						return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
					}

					// ---

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
		*/
	}
}