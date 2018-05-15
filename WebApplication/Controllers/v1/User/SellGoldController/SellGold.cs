﻿using Goldmint.Common;
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

			// ---

			FiatCurrency? fiatCurrency = null;
			CryptoCurrency? cryptoCurrency = null;

			// try parse fiat currency
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				fiatCurrency = fc;
				exchangeCurrency = fc;
			}
			// or crypto currency
			else if (Enum.TryParse(model.Currency, true, out CryptoCurrency cc)) {
				cryptoCurrency = cc;
			}
			else {
				return APIResponse.BadRequest(nameof(model.Currency), "Invalid format");
			}

			// ---

			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount <= 0) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var allowed = false;
			object resultAmount = new double();
			var resultAmountCurrency = "";
			object resultFee = new double();
			var resultFeeCurrency = "";

			// default estimation: GOLD to specified currency
			if (!model.Reversed) {

				// fiat
				if (fiatCurrency != null) {
					var result = await CoreLogic.Finance.Estimation.SellGoldFiat(
						services: HttpContext.RequestServices,
						fiatCurrency: exchangeCurrency,
						goldAmount: inputAmount
					);

					allowed = result.Allowed;

					var mntBalance = await EthereumObserver.GetAddressMntBalance(model.EthAddress);
					var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat(result.ResultCentsAmount, mntBalance);
					resultAmount = (result.ResultCentsAmount - fee) / 100d;
					resultAmountCurrency = exchangeCurrency.ToString().ToUpper();
					resultFee = fee / 100d;
					resultFeeCurrency = exchangeCurrency.ToString().ToUpper();
				}
				// cryptoasset
				else {
					var result = await CoreLogic.Finance.Estimation.SellGoldCrypto(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: exchangeCurrency,
						goldAmount: inputAmount
					);

					allowed = result.Allowed;

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, result.ResultAssetAmount);
					resultAmount = (result.ResultAssetAmount - fee).ToString();
					resultAmountCurrency = cryptoCurrency.Value.ToString().ToUpper();
					resultFee = fee.ToString();
					resultFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();
				}
			}
			// reversed estimation: specified currency to GOLD
			else {

				// fiat
				if (fiatCurrency != null) {

					var mntBalance = await EthereumObserver.GetAddressMntBalance(model.EthAddress);

					if (inputAmount <= long.MaxValue) {

						var fee = CoreLogic.Finance.Estimation.SellingFeeForFiat((long)inputAmount, mntBalance);
						var result = await CoreLogic.Finance.Estimation.SellGoldFiatRev(
							services: HttpContext.RequestServices,
							fiatCurrency: exchangeCurrency,
							requiredFiatAmountWithFeeCents: (long)inputAmount + fee
						);

						allowed = result.Allowed;

						resultAmount = result.ResultGoldAmount.ToString();
						resultAmountCurrency = "GOLD";
						resultFee = fee.ToString();
						resultFeeCurrency = exchangeCurrency.ToString().ToUpper();
					}
				}
				// cryptoasset
				else {

					var fee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(cryptoCurrency.Value, inputAmount);

					var result = await CoreLogic.Finance.Estimation.SellGoldCryptoRev(
						services: HttpContext.RequestServices,
						cryptoCurrency: cryptoCurrency.Value,
						fiatCurrency: exchangeCurrency, 
						requiredCryptoAmountWithFee: inputAmount + fee
					);

					allowed = result.Allowed;

					resultAmount = result.ResultGoldAmount.ToString();
					resultAmountCurrency = "GOLD";
					resultFee = fee.ToString();
					resultFeeCurrency = cryptoCurrency.Value.ToString().ToUpper();
				}
			}

			if (!allowed) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			return APIResponse.Success(
				new EstimateView() {
					Amount = resultAmount,
					AmountCurrency = resultAmountCurrency,
					Fee = resultFee,
					FeeCurrency = resultFeeCurrency,
				}
			);
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
				await TicketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
			}
			catch {
			}

			// TODO: email

			return APIResponse.Success(
				new ConfirmView() { }
			);
		}

	}
}