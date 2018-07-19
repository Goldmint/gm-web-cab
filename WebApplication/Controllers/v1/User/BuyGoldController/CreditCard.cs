using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// USD to GOLD
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
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1 || (!model.Reversed && inputAmount > long.MaxValue)) {
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
			if (!rcfg.Gold.AllowBuyingCreditCard) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var limits = DepositLimits(rcfg, exchangeCurrency);

			var estimation = await Estimation(rcfg, inputAmount, null, exchangeCurrency, model.Reversed, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1 || estimation.ResultCurrencyAmount > long.MaxValue) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(rcfg.Gold.Timeouts.ContractBuyRequest);

			var ticket = await OplogProvider.NewGoldBuyingRequestWithCreditCard(
				userId: user.Id,
				destAddress: model.EthAddress,
				fiatCurrency: exchangeCurrency,
				goldRate: estimation.CentsPerGoldRate,
				centsAmount: (long)estimation.ResultCurrencyAmount
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldBuy,
				Source = exchangeCurrency.ToString().ToUpper(),
				SourceAmount = TextFormatter.FormatAmount((long)estimation.ResultCurrencyAmount),
				Destination = "GOLD",
				DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimation.ResultGoldAmount, Tokens.GOLD.Decimals),
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
			var request = new DAL.Models.BuyGoldRequest() {

				Status = BuyGoldRequestStatus.Unconfirmed,
				Input = BuyGoldRequestInput.CreditCardDeposit,
				RelInputId = card.Id,
				Output = BuyGoldRequestOutput.EthereumAddress,
				EthAddress = model.EthAddress,

				ExchangeCurrency = exchangeCurrency,
				InputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,
				InputExpected = estimation.ResultCurrencyAmount.ToString(),

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RelUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Request #{request.Id}, GOLD/{ exchangeCurrency.ToString().ToUpper() } = { TextFormatter.FormatAmount(estimation.CentsPerGoldRate) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new CreditCardView() {
					RequestId = request.Id,
					Currency = exchangeCurrency.ToString().ToUpper(),
					GoldRate = estimation.CentsPerGoldRate / 100d,
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
					Estimation = estimation.View,
				}
			);
		}

	}
}
