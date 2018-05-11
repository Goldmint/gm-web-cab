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
		public async Task<APIResponse> ForAssetEth([FromBody] AssetEthModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			if (!BigInteger.TryParse(model.Amount, out var amountWei) || amountWei.ToString().Length > 64 || amountWei < 1) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier1) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			var currency = FiatCurrency.Usd;
			var estimation = await CoreLogic.Finance.Estimation.SellGold(
				services: HttpContext.RequestServices,
				exchangeFiatCurrency: currency,
				forCryptoCurrency: CryptoCurrency.Eth,
				goldAmountToSell: amountWei
			);
			if (!estimation.Allowed) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			var estimationFee = CoreLogic.Finance.Estimation.SellingFeeForCrypto(CryptoCurrency.Eth, estimation.TotalAssetAmount);

			var rcfg = RuntimeConfigHolder.Clone();
			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(rcfg.Gold.Timeouts.ContractSellRequest);

			var ticket = await TicketDesk.NewGoldSellingRequestForCryptoasset(
				userId: user.Id,
				cryptoCurrency: CryptoCurrency.Eth,
				destAddress: model.EthAddress,
				fiatCurrency: currency,
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
				Output = SellGoldRequestOutput.Eth,
				OutputAddress = model.EthAddress,

				ExchangeCurrency = currency,
				OutputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,

				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RefUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.SellGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Request #{request.Id}, GOLD/ETH = { TextFormatter.FormatTokenAmount(estimation.CryptoPerGoldRate, Tokens.ETH.Decimals) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = estimation.CentsPerAssetRate / 100d,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					Currency = currency.ToString().ToUpper(),
					EthAmount = (estimation.TotalAssetAmount - estimationFee).ToString(),
					FeeAmount = estimationFee.ToString(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
				}
			);
		}
	}
}