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

		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
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

			// ---

			if (!rcfg.Gold.AllowTradingEth) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			var limits = DepositLimits(rcfg, TradableCurrency.Eth);

			// estimation
			var estimation = await Estimation(rcfg, inputAmount, TradableCurrency.Eth, exchangeCurrency, model.Reversed, 0d, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;

			// request
			var request = new DAL.Models.BuyGoldEth() {
				Status = BuySellGoldRequestStatus.Unconfirmed,
				ExchangeCurrency = exchangeCurrency,
				GoldRateCents = estimation.CentsPerGoldRate,
				EthRateCents = estimation.CentsPerAssetRate,
				TimeCreated = timeNow,
				UserId = user.Id,
			};
			DbContext.BuyGoldEth.Add(request);
			await DbContext.SaveChangesAsync();

			// get a token from eth2gold service
			var contractToken = "";
			{
				try {
					var reply = await Bus.Request(
						Eth2Gold.Subject.Request.OrderCreate,
						new Eth2Gold.Request.OrderCreate() {
							ExternalID = (ulong)request.Id,
						},
						Eth2Gold.Request.OrderCreateReply.Parser
					);
					if (reply.ResultCase == Eth2Gold.Request.OrderCreateReply.ResultOneofCase.Token) {
						if (reply.Token.ToByteArray().Length != 32) {
							throw new Exception($"token is length of {reply.Token.ToByteArray().Length}");
						}
						contractToken = BitConverter.ToString(reply.Token.ToByteArray()).Replace("-", string.Empty);
					} else {
						throw new Exception(reply.Error);
					}
				} catch (Exception e) {
					Logger.Error(e, "Failed to get token from eth2gold service");
					return APIResponse.Failure(APIErrorCode.InternalServerError);
				}
			}

			var assetPerGold = CoreLogic.Finance.Estimation.AssetPerGold(TradableCurrency.Eth, estimation.CentsPerAssetRate, estimation.CentsPerGoldRate);

			return APIResponse.Success(
				new AssetEthView() {
					RequestId = request.Id,
					EthRate = estimation.CentsPerAssetRate / 100d,
					GoldRate = estimation.CentsPerGoldRate / 100d,
					EthPerGoldRate = assetPerGold.ToString(),
					Currency = exchangeCurrency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)timeNow.AddHours(1)).ToUnixTimeSeconds(),
					Estimation = estimation.View,
					ContractToken = contractToken,
				}
			);
		}

	}
}