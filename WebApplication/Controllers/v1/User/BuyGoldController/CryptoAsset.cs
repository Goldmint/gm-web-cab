using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// ETH to GOLD
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("asset/eth")]
		[ProducesResponseType(typeof(AssetEthView), 200)]
		public async Task<APIResponse> ForAssetEth([FromBody] AssetEthModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
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
			var estimation = await CoreLogic.Finance.Estimation.BuyGoldCrypto(
				services: HttpContext.RequestServices,
				cryptoCurrency: CryptoCurrency.Eth,
				fiatCurrency: currency, 
				cryptoAmount: BigInteger.Pow(10, Tokens.ETH.Decimals)
			);
			if (!estimation.Allowed) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var rcfg = RuntimeConfigHolder.Clone();
			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(rcfg.Gold.Timeouts.ContractBuyRequest);

			var ticket = await TicketDesk.NewGoldBuyingRequestForCryptoasset(
				userId: user.Id,
				cryptoCurrency: CryptoCurrency.Eth,
				destAddress: model.EthAddress,
				fiatCurrency: currency,
				inputRate: estimation.CentsPerAssetRate,
				goldRate: estimation.CentsPerGoldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldBuy,
				Source = "ETH",
				SourceAmount = null,
				Destination = "GOLD",
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
			var request = new DAL.Models.BuyGoldRequest() {

				Status = BuyGoldRequestStatus.Unconfirmed,
				Input = BuyGoldRequestInput.ContractEthPayment,
				Output = BuyGoldRequestOutput.EthereumAddress,
				InputAddress = model.EthAddress,

				ExchangeCurrency = currency,
				InputRateCents = estimation.CentsPerAssetRate,
				GoldRateCents = estimation.CentsPerGoldRate,
				
				OplogId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RefUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Request #{request.Id}, GOLD/ETH = { TextFormatter.FormatTokenAmount(estimation.CryptoPerGoldRate, Tokens.ETH.Decimals) }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = estimation.CentsPerAssetRate / 100d,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					EthPerGoldRate = estimation.CryptoPerGoldRate.ToString(),
					Currency = currency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
				}
			);
		}
		
	}
}