using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User
{

	public partial class BuyGoldController : BaseController
	{

		/// <summary>
		/// ETH to GOLD
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("asset/eth")]
		[ProducesResponseType(typeof(AssetEthView), 200)]
		public async Task<APIResponse> AssetEth([FromBody] AssetEthModel model)
		{
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			// try parse amount
			if (!BigInteger.TryParse(model.Amount, out var inputAmount) || inputAmount < 1)
			{
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// try parse fiat currency
			var exchangeCurrency = FiatCurrency.Usd;
			if (Enum.TryParse(model.Currency, true, out FiatCurrency fc))
			{
				exchangeCurrency = fc;
			}

            // ---


		    var rcfg = RuntimeConfigHolder.Clone();

            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier1)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			if (!rcfg.Gold.AllowTradingEth)
			{
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var limits = DepositLimits(rcfg, CryptoCurrency.Eth);

            // get promocode
		    var promoCode = await GetPromoCode(model.PromoCode);

            // must have kyc to use promocode here
            if (promoCode != null && userTier < UserTier.Tier2)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			var estimation = await Estimation(rcfg, inputAmount, CryptoCurrency.Eth, exchangeCurrency, model.Reversed, promoCode, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(rcfg.Gold.Timeouts.ContractBuyRequest);

			var ticket = await OplogProvider.NewGoldBuyingRequestForCryptoasset(
				userId: user.Id,
				cryptoCurrency: CryptoCurrency.Eth,
				destAddress: model.EthAddress,
				fiatCurrency: exchangeCurrency,
				inputRate: estimation.CentsPerAssetRate,
				goldRate: estimation.CentsPerGoldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory()
			{

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldBuy,
				Source = "ETH",
				SourceAmount = null,
				Destination = "GOLD",
				DestinationAmount = null,
				Comment = "", // see below

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires.AddSeconds(rcfg.Gold.Timeouts.ContractBuyRequest), // double time
				UserId = user.Id,
			};

			// add and save
			DbContext.UserFinHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var request = new DAL.Models.BuyGoldRequest() {

				Status = BuyGoldRequestStatus.Unconfirmed,
				Input = BuyGoldRequestInput.ContractEthPayment,
				RelInputId = null,
				Output = BuyGoldRequestOutput.EthereumAddress,
				EthAddress = model.EthAddress,

				ExchangeCurrency = exchangeCurrency,
				InputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,
				InputExpected = estimation.ResultCurrencyAmount.ToString(),

				PromoCodeId = promoCode?.Id,
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

			var assetPerGold = CoreLogic.Finance.Estimation.AssetPerGold(CryptoCurrency.Eth, estimation.CentsPerAssetRate, estimation.CentsPerGoldRate);

			// update comment
			finHistory.Comment = $"Request #{request.Id}, GOLD/ETH = { TextFormatter.FormatTokenAmount(assetPerGold, Tokens.ETH.Decimals) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = estimation.CentsPerAssetRate / 100d,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					EthPerGoldRate = assetPerGold.ToString(),
					Currency = exchangeCurrency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
					Estimation = estimation.View,
				}
			);
		}
		
	}
}