using Goldmint.Common;
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

			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount <= 100 || (cryptoCurrency == null && model.Reversed && inputAmount > long.MaxValue)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var est = await Estimation(inputAmount, cryptoCurrency, exchangeCurrency, model.EthAddress, model.Reversed);
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
					from r in DbContext.SellGoldRequest
					where
						r.Status == SellGoldRequestStatus.Unconfirmed &&
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
			request.Status = SellGoldRequestStatus.Confirmed;

			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch {
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
		}

		[NonAction]
		private async Task<EstimationResult> Estimation(BigInteger inputAmount, CryptoCurrency? cryptoCurrency, FiatCurrency fiatCurrency, string ethAddress, bool reversed) {

			var allowed = false;
			object viewAmount = new double();
			var viewAmountCurrency = "";
			object viewFee = new double();
			var viewFeeCurrency = "";
			var centsPerAsset = 0L;
			var centsPerGold = 0L;

			// default estimation: GOLD to specified currency
			if (!reversed) {

				// fiat
				if (cryptoCurrency == null) {
					var result = await CoreLogic.Finance.Estimation.SellGoldFiat(
						services: HttpContext.RequestServices,
						fiatCurrency: fiatCurrency,
						goldAmount: inputAmount
					);

					allowed = result.Allowed;
					centsPerGold = result.CentsPerGoldRate;

					var mntBalance = ethAddress != null ? await EthereumObserver.GetAddressMntBalance(ethAddress) : BigInteger.Zero;
					var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat(result.ResultCentsAmount, mntBalance);

					viewAmount = (result.ResultCentsAmount - fee) / 100d;
					viewAmountCurrency = fiatCurrency.ToString().ToUpper();
					viewFee = fee / 100d;
					viewFeeCurrency = fiatCurrency.ToString().ToUpper();

					
				}
				// cryptoasset
				else {
					var result = await CoreLogic.Finance.Estimation.SellGoldCrypto(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						goldAmount: inputAmount
					);

					allowed = result.Allowed;
					centsPerGold = result.CentsPerGoldRate;
					centsPerAsset = result.CentsPerAssetRate;

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, result.ResultAssetAmount);

					viewAmount = (result.ResultAssetAmount - fee).ToString();
					viewAmountCurrency = cryptoCurrency.Value.ToString().ToUpper();
					viewFee = fee.ToString();
					viewFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();
				}
			}
			// reversed estimation: specified currency to GOLD
			else {

				// fiat
				if (cryptoCurrency == null) {

					var mntBalance = ethAddress != null ? await EthereumObserver.GetAddressMntBalance(ethAddress) : BigInteger.Zero;

					if (inputAmount <= long.MaxValue) {

						var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat((long)inputAmount, mntBalance);
						var result = await CoreLogic.Finance.Estimation.SellGoldFiatRev(
							services: HttpContext.RequestServices,
							fiatCurrency: fiatCurrency,
							requiredFiatAmountWithFeeCents: (long)inputAmount + fee
						);

						allowed = result.Allowed;
						centsPerGold = result.CentsPerGoldRate;

						viewAmount = result.ResultGoldAmount.ToString();
						viewAmountCurrency = "GOLD";
						viewFee = fee.ToString();
						viewFeeCurrency = fiatCurrency.ToString().ToUpper();
					}
				}
				// cryptoasset
				else {

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, inputAmount);

					var result = await CoreLogic.Finance.Estimation.SellGoldCryptoRev(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: fiatCurrency,
						requiredCryptoAmountWithFee: inputAmount + fee
					);

					allowed = result.Allowed;
					centsPerGold = result.CentsPerGoldRate;
					centsPerAsset = result.CentsPerAssetRate;

					viewAmount = result.ResultGoldAmount.ToString();
					viewAmountCurrency = "GOLD";
					viewFee = fee.ToString();
					viewFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();
				}
			}

			if (!allowed) {
				return null;
			}

			return new EstimationResult() {
				View = new EstimateView() {
					Amount = viewAmount,
					AmountCurrency = viewAmountCurrency,
					Fee = viewFee,
					FeeCurrency = viewFeeCurrency,
				},
				CentsPerAssetRate = centsPerAsset,
				CentsPerGoldRate = centsPerGold,
			};
		}
	}
}