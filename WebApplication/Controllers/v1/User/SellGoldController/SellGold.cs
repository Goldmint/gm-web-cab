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

			var exchangeCurrency = FiatCurrency.USD;

			// ---

			// TODO: use GoldToken safe estimation

			FiatCurrency? fiatCurrency = null;
			CryptoCurrency? cryptoCurrency = null;
			var outputRate = 0L;

			// try parse fiat currency
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				fiatCurrency = fc;
				exchangeCurrency = fc;
				outputRate = 100; // 1:1
			}
			// or crypto currency
			else if (Enum.TryParse(model.Currency, true, out CryptoCurrency cc)) {
				cryptoCurrency = cc;
				outputRate = await CryptoassetRateProvider.GetRate(cc, exchangeCurrency);
			}
			else {
				return APIResponse.BadRequest(nameof(model.Currency), "Invalid format");
			}

			// ---

			var outputDecimals = 0;

			if (fiatCurrency != null) {
				outputDecimals = 2;
			}
			else if (cryptoCurrency == CryptoCurrency.ETH) {
				outputDecimals = Tokens.ETH.Decimals;
			}

			// ---

			BigInteger.TryParse(model.Amount, out var goldAmount);
			if (goldAmount <= 0) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var goldRate = await GoldRateCached.GetGoldRate(exchangeCurrency);

			// ---

			var exchangeAmount = goldAmount * new BigInteger(goldRate);
			var outputAmount = exchangeAmount * BigInteger.Pow(10, outputDecimals) / outputRate / BigInteger.Pow(10, Tokens.GOLD.Decimals);

			return APIResponse.Success(
				new EstimateView() {
					Amount = outputAmount.ToString(),
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