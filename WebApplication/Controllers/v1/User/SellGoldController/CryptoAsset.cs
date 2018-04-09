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
		/// GOLD => Contract => ETH
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

			var currency = FiatCurrency.USD;

			// TODO: use safe rate provider
			var ethRate = await CryptoassetRateProvider.GetRate(CryptoCurrency.ETH, currency);
			var goldRate = await GoldRateProvider.GetRate(currency);

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(AppConfig.Constants.TimeLimits.SellGoldForEthRequestTimeoutSec);

			var ticket = await TicketDesk.NewGoldSellingRequestForCryptoasset(
				userId: user.Id,
				cryptoCurrency: CryptoCurrency.ETH,
				destAddress: model.EthAddress,
				fiatCurrency: currency,
				outputRate: ethRate,
				goldRate: goldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldSell,
				Source = "GOLD",
				Destination = "ETH",
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
				OutputRateCents = ethRate,
				GoldRateCents = goldRate,

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
			finHistory.Comment = $"Request #{request.Id}, {TextFormatter.FormatAmount(goldRate, currency)} per GOLD, {TextFormatter.FormatAmount(ethRate, currency)} per ETH";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = ethRate / 100d,
					GoldRate = goldRate / 100d,
					Currency = currency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
				}
			);
		}
	}
}