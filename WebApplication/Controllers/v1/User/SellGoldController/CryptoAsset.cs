using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SellGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SellGoldController : BaseController {

		/// <summary>
		/// GOLD to ETH
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("asset/eth")]
		[ProducesResponseType(typeof(AssetEthView), 200)]
		public async Task<APIResponse> AssetEth([FromBody] AssetEthModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// try parse amount
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// try parse fiat currency
			var exchangeCurrency = FiatCurrency.Usd;
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc)) {
				exchangeCurrency = fc;
			}

			// ---

			var rcfg = RuntimeConfigHolder.Clone();

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			if (!rcfg.Gold.AllowTradingEth) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var limits = WithdrawalLimits(rcfg, EthereumToken.Eth);

			var estimation = await Estimation(rcfg, inputAmount, EthereumToken.Eth, exchangeCurrency, model.EthAddress, model.Reversed, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;

			var ticket = await OplogProvider.NewGoldSellingRequestForCryptoasset(
				userId: user.Id,
				ethereumToken: EthereumToken.Eth,
				destAddress: model.EthAddress,
				fiatCurrency: exchangeCurrency,
				outputRate: estimation.CentsPerAssetRate,
				goldRate: estimation.CentsPerGoldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldSell,
				Source = "GOLD",
				SourceAmount = null,
				Destination = "ETH",
				DestinationAmount = null,
				Comment = "", // see below

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = null,
				UserId = user.Id,
			};

			// add and save
			DbContext.UserFinHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var request = new DAL.Models.SellGoldRequest() {

				Status = SellGoldRequestStatus.Unconfirmed,
				Input = SellGoldRequestInput.SumusGoldBurning,
				Output = SellGoldRequestOutput.EthAddress,
				RelOutputId = null,
				EthAddress = model.EthAddress,

				ExchangeCurrency = exchangeCurrency,
				OutputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,
				InputExpected = estimation.ResultGoldAmount.ToString(),

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeNow.AddDays(1),
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RelUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.SellGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			var assetPerGold = CoreLogic.Finance.Estimation.AssetPerGold(EthereumToken.Eth, estimation.CentsPerAssetRate, estimation.CentsPerGoldRate);

			// update comment
			finHistory.Comment = $"Request #{request.Id} | GOLD/ETH = { TextFormatter.FormatTokenAmount(assetPerGold, TokensPrecision.Ethereum) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = estimation.CentsPerAssetRate / 100d,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					Currency = exchangeCurrency.ToString().ToUpper(),
					EthPerGoldRate = assetPerGold.ToString(),
					Estimation = estimation.View,
				}
			);
		}
	}
}