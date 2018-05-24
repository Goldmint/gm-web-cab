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
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1 || (cryptoCurrency == null && !model.Reversed && inputAmount > long.MaxValue)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// TODO: exchange amount limits

			// ---

			var est = await Estimation(inputAmount, cryptoCurrency, exchangeCurrency, model.Reversed);
			if (est == null) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			return APIResponse.Success(est.View);
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
			.Include(_ => _.RefUserFinHistory)
			.AsTracking()
			.FirstOrDefaultAsync()
			;
			
			// request not exists
			if (request == null) {
				return APIResponse.BadRequest(nameof(model.RequestId), "Invalid id");
			}

			// mark request for processing
			request.RefUserFinHistory.Status = UserFinHistoryStatus.Manual;
			request.Status = BuyGoldRequestStatus.Confirmed;

			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch {
			}

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
					.FirstOrDefaultAsync()
				;
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

			// TODO: email

			return APIResponse.Success(
				new ConfirmView() { }
			);
		}

		// ---

		internal class EstimationResult {

			public EstimateView View { get; set; }
			public long CentsPerAssetRate { get; set; }
			public long CentsPerGoldRate { get; set; }
			public BigInteger ResultCurrencyAmount { get; set; }
			public BigInteger ResultGoldAmount { get; set; }
		}

		[NonAction]
		private async Task<EstimationResult> Estimation(BigInteger inputAmount, CryptoCurrency? cryptoCurrency, FiatCurrency fiatCurrency, bool reversed) {

			bool allowed = false;
			
			var centsPerAsset = 0L;
			var centsPerGold = 0L;
			var resultCurrencyAmount = BigInteger.Zero;
			var resultGoldAmount = BigInteger.Zero;

			object viewAmount = null;
			string viewAmountCurrency = "";

			// default estimation: specified currency to GOLD
			if (!reversed) {

				// fiat
				if (cryptoCurrency == null) {
					var res = await CoreLogic.Finance.Estimation.BuyGoldFiat(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						fiatAmountCents: (long)inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultCurrencyAmount = res.ResultCentsAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = res.ResultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";
				}

				// cryptoasset
				else {
					var res = await CoreLogic.Finance.Estimation.BuyGoldCrypto(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						cryptoAmount: inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultCurrencyAmount = res.ResultAssetAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = res.ResultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";
				}
			}
			// reversed estimation: GOLD to specified currency
			else {

				// fiat
				if (cryptoCurrency == null) {
					var res = await CoreLogic.Finance.Estimation.BuyGoldFiatRev(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						requiredGoldAmount: inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					resultCurrencyAmount = res.ResultCentsAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = res.ResultCentsAmount / 100d;
					viewAmountCurrency = fiatCurrency.ToString().ToUpper();
				}

				// cryptoasset
				else {
					var res = await CoreLogic.Finance.Estimation.BuyGoldCryptoRev(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						requiredGoldAmount: inputAmount
					);

					allowed = res.Allowed;
					centsPerGold = res.CentsPerGoldRate;
					centsPerAsset = res.CentsPerAssetRate;
					resultCurrencyAmount = res.ResultAssetAmount;
					resultGoldAmount = res.ResultGoldAmount;

					viewAmount = res.ResultAssetAmount.ToString();
					viewAmountCurrency = cryptoCurrency.Value.ToString().ToUpper();
				}
			}

			if (!allowed) {
				return null;
			}

			return new EstimationResult() {
				View = new EstimateView() {
					Amount = viewAmount,
					AmountCurrency = viewAmountCurrency,
				},
				CentsPerAssetRate = centsPerAsset,
				CentsPerGoldRate = centsPerGold,
				ResultCurrencyAmount = resultCurrencyAmount,
				ResultGoldAmount = resultGoldAmount,
			};
		}
	}
}