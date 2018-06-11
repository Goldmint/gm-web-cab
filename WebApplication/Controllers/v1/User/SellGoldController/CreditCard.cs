using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SellGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SellGoldController : BaseController {

		/// <summary>
		/// GOLD to USD
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("ccard")]
		[ProducesResponseType(typeof(CreditCardView), 200)]
		public async Task<APIResponse> CreditCard([FromBody] CreditCardModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// try parse amount
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1 || (model.Reversed && inputAmount > long.MaxValue)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// try parse fiat currency
			var exchangeCurrency = FiatCurrency.Usd;
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				exchangeCurrency = fc;
			}

			// ---

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// extra access
			if (HostingEnvironment.IsProduction() && (user.AccessRights & (long)AccessRights.ClientExtraAccess) != (long)AccessRights.ClientExtraAccess) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get the card
			var card = await (
					from c in DbContext.UserCreditCard
					where
						c.UserId == user.Id &&
						c.Id == model.CardId &&
						c.State == CardState.Verified
					select c
				)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;
			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
			}

			// ---

			var rcfg = RuntimeConfigHolder.Clone();
			if (!rcfg.Gold.AllowTradingCreditCard) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var limits = WithdrawalLimits(rcfg, exchangeCurrency);

			var estimation = await Estimation(rcfg, inputAmount, null, exchangeCurrency, model.EthAddress, model.Reversed, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1 || estimation.ResultCurrencyAmount > long.MaxValue) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(rcfg.Gold.Timeouts.ContractSellRequest);

			var ticket = await OplogProvider.NewGoldSellingRequestWithCreditCard(
				userId: user.Id,
				destAddress: model.EthAddress,
				fiatCurrency: exchangeCurrency,
				goldRate: estimation.CentsPerGoldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldSell,
				Source = "GOLD",
				SourceAmount = TextFormatter.FormatTokenAmountFixed(estimation.ResultGoldAmount, Tokens.GOLD.Decimals),
				Destination = exchangeCurrency.ToString().ToUpper(),
				DestinationAmount = TextFormatter.FormatAmount((long)estimation.ResultCurrencyAmount),
				Comment = "", // see below

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				UserId = user.Id,
			};

			// add and save
			DbContext.UserFinHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var request = new DAL.Models.SellGoldRequest() {

				Status = SellGoldRequestStatus.Unconfirmed,
				Input = SellGoldRequestInput.ContractGoldBurning,
				Output = SellGoldRequestOutput.CreditCard,
				RelOutputId = card.Id,
				EthAddress = model.EthAddress,

				ExchangeCurrency = exchangeCurrency,
				OutputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,
				InputExpected = estimation.ResultGoldAmount.ToString(),

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RelUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.SellGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Request #{request.Id}, GOLD/{ exchangeCurrency.ToString().ToUpper() } = { TextFormatter.FormatAmount(estimation.CentsPerGoldRate) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new CreditCardView() {
					RequestId = request.Id,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					Currency = exchangeCurrency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
					Estimation = estimation.View,
				}
			);
		}
	}
}