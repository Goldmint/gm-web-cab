using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SellGoldModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/gold/sell")]
	public partial class SellGoldController : BaseController {

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

			var exchangeCurrency = FiatCurrency.Usd;
			CryptoCurrency? cryptoCurrency = null;

			// try parse fiat currency
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				exchangeCurrency = fc;
			}
			// or crypto currency
			else if (Enum.TryParse(model.Currency, true, out CryptoCurrency cc)) {
				cryptoCurrency = cc;
			}
			else {
				return APIResponse.BadRequest(nameof(model.Currency), "Invalid format");
			}

			// try parse amount
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1 || (cryptoCurrency == null && model.Reversed && inputAmount > long.MaxValue)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var rcfg = RuntimeConfigHolder.Clone();

			var limits = cryptoCurrency != null
				? WithdrawalLimits(rcfg, cryptoCurrency.Value)
				: WithdrawalLimits(rcfg, exchangeCurrency)
			;

			var estimation = await Estimation(rcfg, inputAmount, cryptoCurrency, exchangeCurrency, model.EthAddress, model.Reversed, limits.Min, limits.Max);
			if (!estimation.TradingAllowed) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			return APIResponse.Success(estimation.View);
		}

		/// <summary>
		/// Confirm request
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
			var userLocale = GetUserLocale();
			var agent = GetUserAgentInfo();

			// ---

			var request = await (
					from r in DbContext.SellGoldRequest
					where
						r.Status == SellGoldRequestStatus.Unconfirmed &&
						r.Id == model.RequestId &&
						r.UserId == user.Id &&
						r.TimeExpires > DateTime.UtcNow
					select r
				)
				.Include(_ => _.RelUserFinHistory)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			// request not exists
			if (request == null) {
				return APIResponse.BadRequest(nameof(model.RequestId), "Invalid id");
			}

			// activity
			var userActivity = CoreLogic.User.CreateUserActivity(
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"Gold selling request #{request.Id} confirmed",
				ip: agent.Ip,
				agent: agent.Agent,
				locale: userLocale
			);
			DbContext.UserActivity.Add(userActivity);
			await DbContext.SaveChangesAsync();

			// mark request for processing
			request.RelUserFinHistory.Status = UserFinHistoryStatus.Manual;
			request.RelUserFinHistory.RelUserActivityId = userActivity.Id;
			request.Status = SellGoldRequestStatus.Confirmed;
			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch {
			}

			// TODO: email?

			return APIResponse.Success(
				new ConfirmView() { }
			);
		}

		// ---

		internal class EstimationResult {

			public bool TradingAllowed { get; set; }
			public bool IsLimitExceeded { get; set; }
			public EstimateView View { get; set; }
			public long CentsPerAssetRate { get; set; }
			public long CentsPerGoldRate { get; set; }
			public BigInteger ResultGoldAmount { get; set; }
			public BigInteger ResultCurrencyAmount { get; set; }
		}

		[NonAction]
		private async Task<EstimationResult> Estimation(RuntimeConfig rcfg, BigInteger inputAmount, CryptoCurrency? cryptoCurrency, FiatCurrency fiatCurrency, string ethAddress, bool reversed, BigInteger withdrawalLimitMin, BigInteger withdrawalLimitMax) {

			var allowed = false;
			
			var centsPerAsset = 0L;
			var centsPerGold = 0L;
			var resultGoldAmount = BigInteger.Zero;
			var resultCurrencyAmount = BigInteger.Zero;
			var resultCurrencyAmountMinusFee = BigInteger.Zero;

			object viewAmount = null;
			var viewAmountCurrency = "";
			object viewFee = null;
			var viewFeeCurrency = "";

			var limitsData = (EstimateLimitsView)null;

			// default estimation: GOLD to specified currency
			if (!reversed) {

				// fiat
				if (cryptoCurrency == null) {
					var res = await CoreLogic.Finance.Estimation.SellGoldFiat(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						goldAmount: inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultGoldAmount = res.ResultGoldAmount;
					resultCurrencyAmount = res.ResultCentsAmount;

					var mntBalance = ethAddress != null ? await EthereumObserver.GetAddressMntBalance(ethAddress) : BigInteger.Zero;
					var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat(res.ResultCentsAmount, mntBalance);
					resultCurrencyAmountMinusFee = res.ResultCentsAmount - fee;

					viewAmount = (long)resultCurrencyAmountMinusFee / 100d;
					viewAmountCurrency = fiatCurrency.ToString().ToUpper();
					viewFee = fee / 100d;
					viewFeeCurrency = fiatCurrency.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = (long)withdrawalLimitMin / 100d,
						Max = (long)withdrawalLimitMax / 100d,
						Cur = (long)resultCurrencyAmountMinusFee / 100d,
					};
				}
				// cryptoasset
				else {
					var res = await CoreLogic.Finance.Estimation.SellGoldCrypto(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						goldAmount: inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultGoldAmount = res.ResultGoldAmount;
					resultCurrencyAmount = res.ResultAssetAmount;

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, res.ResultAssetAmount);
					resultCurrencyAmountMinusFee = res.ResultAssetAmount - fee;

					viewAmount = resultCurrencyAmountMinusFee.ToString();
					viewAmountCurrency = cryptoCurrency.Value.ToString().ToUpper();
					viewFee = fee.ToString();
					viewFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = withdrawalLimitMin.ToString(),
						Max = withdrawalLimitMax.ToString(),
						Cur = resultCurrencyAmountMinusFee.ToString(),
					};
				}
			}
			// reversed estimation: specified currency to GOLD
			else {

				// fiat
				if (cryptoCurrency == null) {

					var mntBalance = ethAddress != null ? await EthereumObserver.GetAddressMntBalance(ethAddress) : BigInteger.Zero;

					var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat((long)inputAmount, mntBalance);
					var res = await CoreLogic.Finance.Estimation.SellGoldFiatRev(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						requiredFiatAmountWithFeeCents: (long)inputAmount + fee
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultGoldAmount = res.ResultGoldAmount;
					resultCurrencyAmount = res.ResultCentsAmount;
					resultCurrencyAmountMinusFee = inputAmount;

					viewAmount = res.ResultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";
					viewFee = fee.ToString();
					viewFeeCurrency = fiatCurrency.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = (long)withdrawalLimitMin / 100d,
						Max = (long)withdrawalLimitMax / 100d,
						Cur = (long)resultCurrencyAmountMinusFee / 100d,
					};
				}
				// cryptoasset
				else {

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, inputAmount);

					var res = await CoreLogic.Finance.Estimation.SellGoldCryptoRev(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						requiredCryptoAmountWithFee: inputAmount + fee
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultGoldAmount = res.ResultGoldAmount;
					resultCurrencyAmount = res.ResultAssetAmount;
					resultCurrencyAmountMinusFee = inputAmount;

					viewAmount = res.ResultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";
					viewFee = fee.ToString();
					viewFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = withdrawalLimitMin.ToString(),
						Max = withdrawalLimitMax.ToString(),
						Cur = resultCurrencyAmountMinusFee.ToString(),
					};
				}
			}

			var limitExceeded = resultCurrencyAmountMinusFee < withdrawalLimitMin || resultCurrencyAmountMinusFee > withdrawalLimitMax;

			return new EstimationResult() {
				TradingAllowed = allowed,
				IsLimitExceeded = limitExceeded,
				View = new EstimateView() {
					Amount = viewAmount,
					AmountCurrency = viewAmountCurrency,
					Fee = viewFee,
					FeeCurrency = viewFeeCurrency,
					Limits = limitsData,
				},
				CentsPerAssetRate = centsPerAsset,
				CentsPerGoldRate = centsPerGold,
				ResultGoldAmount = resultGoldAmount,
				ResultCurrencyAmount = resultCurrencyAmount,
			};
		}

		// ---

		public class WithdrawalLimitsResult {

			public BigInteger Min { get; set; }
			public BigInteger Max { get; set; }
		}

		[NonAction]
		public static WithdrawalLimitsResult WithdrawalLimits(RuntimeConfig rcfg, CryptoCurrency cryptoCurrency) {

			var cryptoAccuracy = 8;
			var decimals = cryptoAccuracy;
			var min = 0d;
			var max = 0d;

			if (cryptoCurrency == CryptoCurrency.Eth) {
				decimals = Tokens.ETH.Decimals;
				min = rcfg.Gold.PaymentMehtods.EthWithdrawMinEther;
				max = rcfg.Gold.PaymentMehtods.EthWithdrawMaxEther;
			}

			if (min > 0 && max > 0) {
				var pow = BigInteger.Pow(10, decimals - cryptoAccuracy);
				return new WithdrawalLimitsResult() {
					Min = new BigInteger((long)Math.Floor(min * Math.Pow(10, cryptoAccuracy))) * pow,
					Max = new BigInteger((long)Math.Floor(max * Math.Pow(10, cryptoAccuracy))) * pow,
				};
			}
			
			throw new NotImplementedException($"{cryptoCurrency} currency is not implemented");
		}

		[NonAction]
		public static WithdrawalLimitsResult WithdrawalLimits(RuntimeConfig rcfg, FiatCurrency fiatCurrency) {

			var min = 0d;
			var max = 0d;

			if (fiatCurrency == FiatCurrency.Usd) {
				min = rcfg.Gold.PaymentMehtods.CreditCardWithdrawMinUsd;
				max = rcfg.Gold.PaymentMehtods.CreditCardWithdrawMaxUsd;
			}

			if (min > 0 && max > 0) {
				return new WithdrawalLimitsResult() {
					Min = (long)Math.Floor(min * 100d),
					Max = (long)Math.Floor(max * 100d),
				};
			}

			throw new NotImplementedException($"{fiatCurrency} currency is not implemented");
		}
	}
}