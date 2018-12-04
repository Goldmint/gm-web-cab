using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.DAL;
using Goldmint.DAL.Models.PromoCode;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/gold/buy")]
	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// Estimate
		/// </summary>
		//[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[AnonymousAccess]
		[HttpPost, Route("estimate")]
		[ProducesResponseType(typeof(EstimateView), 200)]
		public async Task<APIResponse> Estimate([FromBody] EstimateModel model) {

			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var exchangeCurrency = FiatCurrency.Usd;
			EthereumToken? ethereumToken = null;

			// try parse fiat currency
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				exchangeCurrency = fc;
			}
			// or crypto currency
			else if (Enum.TryParse(model.Currency, true, out EthereumToken cc)) {
				ethereumToken = cc;
			}
			else {
				return APIResponse.BadRequest(nameof(model.Currency), "Invalid format");
			}

			// try parse amount
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1 || (ethereumToken == null && !model.Reversed && inputAmount > long.MaxValue)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var userOrNull = await GetUserFromDb();
			var rcfg = RuntimeConfigHolder.Clone();

			// get user limits
			var limits = ethereumToken != null
				? DepositLimits(rcfg, ethereumToken.Value)
				: await DepositLimits(rcfg, DbContext, userOrNull?.Id, exchangeCurrency);

			// check promocode
			PromoCode promoCode = null;
			if (rcfg.Gold.AllowPromoCodes) {
				var codeStatus = await GetPromoCodeStatus(model.PromoCode);

				if (codeStatus.Valid == false) {
					if (codeStatus.ErrorCode == APIErrorCode.PromoCodeNotEnter)
						promoCode = null;
					else {
						return APIResponse.BadRequest(codeStatus.ErrorCode);
					}
				}
				else {
					//if (await GetUserTier() != UserTier.Tier2) {
					//	return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
					//}
					promoCode = await DbContext.PromoCode
							.AsNoTracking()
							.FirstOrDefaultAsync(_ => _.Code == model.PromoCode.ToUpper())
						;
				}
			}

			// estimate
			var estimation = await Estimation(rcfg, inputAmount, ethereumToken, exchangeCurrency, model.Reversed, promoCode?.DiscountValue ?? 0d, limits.Min, limits.Max);
			if (!estimation.TradingAllowed) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			// promocode limit
			if (promoCode != null) {

				var limit = new BigInteger(promoCode.Limit * (decimal) Math.Pow(10, TokensPrecision.EthereumGold));
				if (limit < estimation.ResultGoldAmount) {
					return APIResponse.BadRequest(APIErrorCode.PromoCodeLimitExceeded);
				}
				estimation.View.Discount = promoCode.DiscountValue;
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

			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userLocale = GetUserLocale();
			var agent = GetUserAgentInfo();

			// ---

			var request = await (
				from r in DbContext.BuyGoldRequest
				where
					r.Status == BuyGoldRequestStatus.Unconfirmed &&
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

			if (model.PromoCode != null) {
				//get promocode and check again then mark it as used
				await MarkPromoCodeUsed(model.PromoCode, user.Id, model.RequestId);
			}

			// activity
			var userActivity = CoreLogic.User.CreateUserActivity(
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"Gold buying request #{request.Id} confirmed",
				ip: agent.Ip,
				agent: agent.Agent,
				locale: userLocale
			);
			DbContext.UserActivity.Add(userActivity);
			await DbContext.SaveChangesAsync();

			// mark request for processing
			request.RelUserFinHistory.Status = UserFinHistoryStatus.Manual;
			request.RelUserFinHistory.RelUserActivityId = userActivity.Id;
			request.Status = BuyGoldRequestStatus.Confirmed;

			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch { }

			// credit card
			if (request.Input == BuyGoldRequestInput.CreditCardDeposit) {

				// check
				if (request.RelInputId == null) {
					throw new Exception($"RelInputId is invalid at #{ request.Id }");
				}
				if (!long.TryParse(request.InputExpected, out var amount)) {
					throw new Exception($"Amount is invalid at #{ request.Id }");
				}

				// get the card
				var card = await (
						from c in DbContext.UserCreditCard
						where
							c.UserId == user.Id &&
							c.Id == request.RelInputId &&
							c.State == CardState.Verified
						select c
					)
					.AsNoTracking()
					.FirstOrDefaultAsync();
				if (card == null) {
					return APIResponse.BadRequest(nameof(model.RequestId), "Invalid id");
				}

				// enqueue payment
				var payment = await CoreLogic.Finance.The1StPaymentsProcessing.CreateDepositPayment(
					services: HttpContext.RequestServices,
					card: card,
					currency: request.ExchangeCurrency,
					amountCents: amount,
					buyRequestId: request.Id,
					oplogId: request.OplogId
				);
				payment.Status = CardPaymentStatus.Pending;
				DbContext.CreditCardPayment.Add(payment);
				await DbContext.SaveChangesAsync();

			}

			// TODO: email?

			return APIResponse.Success(
				new ConfirmView() { }
			);
		}

		// ---

		[NonAction]
		private async Task<PromoCodeStatus> GetPromoCodeStatus(string str) {
			if (string.IsNullOrEmpty(str))
				return new PromoCodeStatus {
					Valid = false,
					ErrorCode = APIErrorCode.PromoCodeNotEnter
				};

			var code = await DbContext.PromoCode.AsNoTracking().FirstOrDefaultAsync(
				_ => _.Code == str.ToUpper());

			if (code == null)
				return new PromoCodeStatus {
					Valid = false,
					ErrorCode = APIErrorCode.PromoCodeNotFound
				};

			if (code.TimeExpires < DateTime.UtcNow)
				return new PromoCodeStatus {
					Valid = false,
					ErrorCode = APIErrorCode.PromoCodeExpired
				};

			if (code.UsageType == PromoCodeUsageType.Single) {
				var used = await DbContext.UsedPromoCodes.AsNoTracking().FirstOrDefaultAsync(
					_ => _.PromoCodeId == code.Id);

				if (used != null)
					return new PromoCodeStatus {
						Valid = false,
						ErrorCode = APIErrorCode.PromoCodeIsUsed
					};
			}
			if (code.UsageType == PromoCodeUsageType.Multiple) {
				var user = await GetUserFromDb();
				var used = await DbContext.UsedPromoCodes.AsNoTracking().FirstOrDefaultAsync(
					_ => _.PromoCodeId == code.Id &&
						 _.UserId == user.Id);

				if (used != null)
					return new PromoCodeStatus {
						Valid = false,
						ErrorCode = APIErrorCode.PromoCodeIsUsed
					};
			}

			return new PromoCodeStatus {
				Valid = true
			};
		}

		[NonAction]
		private async Task MarkPromoCodeUsed(string str, long userId, long requestId) {
			var pc = await (
					from c in DbContext.PromoCode
					where
						c.Code == str.ToUpper()
					select c
				)
				.AsTracking()
				.FirstOrDefaultAsync();

			if (pc == null)
				throw new Exception($"PromoCode not found for #{ requestId }");

			await DbContext.AddAsync(new UsedPromoCodes() {
				PromoCodeId = pc.Id,
				TimeUsed = DateTime.UtcNow,
				UserId = userId
			});

			await DbContext.SaveChangesAsync();
		}

		// ---

		internal class PromoCodeStatus {
			public bool Valid { get; set; }
			public APIErrorCode ErrorCode { get; set; }
		}

		internal class EstimationResult {
			public bool TradingAllowed { get; set; }
			public bool IsLimitExceeded { get; set; }
			public EstimateView View { get; set; }
			public long CentsPerAssetRate { get; set; }
			public long CentsPerGoldRate { get; set; }
			public BigInteger ResultCurrencyAmount { get; set; }
			public BigInteger ResultGoldAmount { get; set; }
		}

		[NonAction]
		private async Task<EstimationResult> Estimation(
			RuntimeConfig rcfg,
			BigInteger inputAmount,
			EthereumToken? ethereumToken,
			FiatCurrency fiatCurrency,
			bool reversed,
			double discount,
			BigInteger depositLimitMin,
			BigInteger depositLimitMax
		) {

			bool allowed = false;

			var centsPerAsset = 0L;
			var centsPerGold = 0L;
			var resultCurrencyAmount = BigInteger.Zero;
			var resultGoldAmount = BigInteger.Zero;

			object viewAmount = null;
			var viewAmountCurrency = "";

			var limitsData = (EstimateLimitsView)null;

			// default estimation: specified currency to GOLD
			if (!reversed) {
				// fiat
				if (ethereumToken == null) {

					var res = await CoreLogic.Finance.Estimation.BuyGoldFiat(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						fiatAmountCents: (long)inputAmount,
						discount: discount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultCurrencyAmount = inputAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = resultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = (long)depositLimitMin / 100d,
						Max = (long)depositLimitMax / 100d,
						Cur = (long)resultCurrencyAmount / 100d,
					};
				}

				// cryptoasset
				else {
					var res = await CoreLogic.Finance.Estimation.BuyGoldCrypto(
						services: HttpContext.RequestServices,
						ethereumToken: ethereumToken.Value,
						fiatCurrency: fiatCurrency,
						cryptoAmount: inputAmount,
						discount: discount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultCurrencyAmount = inputAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = resultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = depositLimitMin.ToString(),
						Max = depositLimitMax.ToString(),
						Cur = resultCurrencyAmount.ToString(),
					};
				}

			}
			// reversed estimation: GOLD to specified currency
			else {
				// fiat
				if (ethereumToken == null) {
					var res = await CoreLogic.Finance.Estimation.BuyGoldFiatRev(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						requiredGoldAmount: inputAmount,
						discount: discount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultCurrencyAmount = res.ResultCentsAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = (long)resultCurrencyAmount / 100d;
					viewAmountCurrency = fiatCurrency.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = (long)depositLimitMin / 100d,
						Max = (long)depositLimitMax / 100d,
						Cur = (long)resultCurrencyAmount / 100d,
					};
				}

				// cryptoasset
				else {
					var res = await CoreLogic.Finance.Estimation.BuyGoldCryptoRev(
						services: HttpContext.RequestServices,
						ethereumToken: ethereumToken.Value,
						fiatCurrency: fiatCurrency,
						requiredGoldAmount: inputAmount,
						discount: discount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultCurrencyAmount = res.ResultAssetAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = resultCurrencyAmount.ToString();
					viewAmountCurrency = ethereumToken.Value.ToString().ToUpper();

					limitsData = new EstimateLimitsView() {
						Currency = fiatCurrency.ToString().ToUpper(),
						Min = depositLimitMin.ToString(),
						Max = depositLimitMax.ToString(),
						Cur = resultCurrencyAmount.ToString(),
					};
				}
			}

			var limitExceeded = resultCurrencyAmount < depositLimitMin || resultCurrencyAmount > depositLimitMax;

			return new EstimationResult() {
				TradingAllowed = allowed,
				IsLimitExceeded = limitExceeded,
				View = new EstimateView() {
					Amount = viewAmount,
					AmountCurrency = viewAmountCurrency,
					Limits = limitsData,
				},
				CentsPerAssetRate = centsPerAsset,
				CentsPerGoldRate = centsPerGold,
				ResultCurrencyAmount = resultCurrencyAmount,
				ResultGoldAmount = resultGoldAmount,
			};
		}

		// ---

		public class DepositLimitsResult {

			public BigInteger Min { get; set; }
			public BigInteger Max { get; set; }
			public BigInteger AccountMax { get; set; }
			public BigInteger AccountUsed { get; set; }
		}

		[NonAction]
		public static DepositLimitsResult DepositLimits(RuntimeConfig rcfg, EthereumToken ethereumToken) {
			//TODO: -> const
			const int cryptoAccuracy = 8;
			var decimals = cryptoAccuracy;
			var min = 0d;
			var max = 0d;

			if (ethereumToken == EthereumToken.Eth) {
				decimals = TokensPrecision.Ethereum;
				min = rcfg.Gold.PaymentMehtods.EthDepositMinEther;
				max = rcfg.Gold.PaymentMehtods.EthDepositMaxEther;
			}

			if (min > 0 && max > 0) {
				var pow = BigInteger.Pow(10, decimals - cryptoAccuracy);
				return new DepositLimitsResult() {
					Min = new BigInteger((long)Math.Floor(min * Math.Pow(10, cryptoAccuracy))) * pow,
					Max = new BigInteger((long)Math.Floor(max * Math.Pow(10, cryptoAccuracy))) * pow,
				};
			}

			throw new NotImplementedException($"{ethereumToken} currency is not implemented");
		}

		[NonAction]
		public static async Task<DepositLimitsResult> DepositLimits(RuntimeConfig rcfg, ApplicationDbContext dbContext, long? userId, FiatCurrency fiatCurrency) {

			var min = 0d;
			var max = 0d;
			var accMax = 0d;
			var accUsed = 0d;

			var userLimits = userId != null
				? await CoreLogic.User.GetUserLimits(dbContext, userId.Value)
				: (CoreLogic.User.UpdateUserLimitsData) null;

			switch (fiatCurrency) {

				case FiatCurrency.Usd:
					min = rcfg.Gold.PaymentMehtods.CreditCardDepositMinUsd;
					max = rcfg.Gold.PaymentMehtods.CreditCardDepositMaxUsd;

					// has limit
					accMax = rcfg.Gold.PaymentMehtods.FiatUserDepositLimitUsd;
					if (accMax > 0) {
						accUsed = (userLimits?.FiatUsdDeposited ?? 0) / 100d;
						max = Math.Min(
							max,
							Math.Max(0, accMax - accUsed)
						);
					}
					break;

				default:
					throw new NotImplementedException($"{fiatCurrency} currency is not implemented");
			}

			return new DepositLimitsResult() {
				Min = (long)Math.Floor(min * 100d),
				Max = (long)Math.Floor(max * 100d),
				AccountMax = (long)Math.Floor(accMax * 100d),
				AccountUsed = (long)Math.Floor(accUsed * 100d),
			};
		}
	}
}