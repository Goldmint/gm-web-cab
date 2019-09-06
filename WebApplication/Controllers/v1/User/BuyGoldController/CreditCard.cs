﻿using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Linq;
using Goldmint.DAL.Models.PromoCode;
using Microsoft.EntityFrameworkCore;
using Goldmint.CoreLogic.Services.Bus.Nats;
using Goldmint.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// USD to GOLD
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
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

			var rcfg = RuntimeConfigHolder.Clone();
			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier1) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			var limits = await DepositLimits(rcfg, DbContext, user.Id, exchangeCurrency);

			// estimation
			var estimation = await Estimation(rcfg, inputAmount, null, exchangeCurrency, model.Reversed, 0d, limits.Min, limits.Max);
			if (!estimation.TradingAllowed || estimation.ResultCurrencyAmount < 1 || estimation.ResultCurrencyAmount > long.MaxValue) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}
			if (estimation.IsLimitExceeded) {
				return APIResponse.BadRequest(APIErrorCode.TradingExchangeLimit, estimation.View.Limits);
			}

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(300);

			var ticket = await OplogProvider.NewGoldBuyingRequestWithCreditCard(
				userId: user.Id,
				destAddress: model.SumusAddress,
				fiatCurrency: exchangeCurrency,
				goldRate: estimation.CentsPerGoldRate,
				centsAmount: (long)estimation.ResultCurrencyAmount,
				promoCode: null
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Processing,
				Type = UserFinHistoryType.GoldBuy,
				Source = exchangeCurrency.ToString().ToUpper(),
				SourceAmount = TextFormatter.FormatAmount((long)estimation.ResultCurrencyAmount),
				Destination = "GOLD",
				DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimation.ResultGoldAmount, TokensPrecision.EthereumGold),
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
			var request = new DAL.Models.BuyGoldFiat() {
				Status = userTier < UserTier.Tier2? SellGoldRequestStatus.Unconfirmed: SellGoldRequestStatus.Confirmed,
				FiatAmount = (long)estimation.ResultCurrencyAmount,
				GoldAmount = estimation.ResultGoldAmount.FromSumus(),
				Destination = model.SumusAddress,

				ExchangeCurrency = exchangeCurrency,
				GoldRateCents = estimation.CentsPerGoldRate,

				OplogId = ticket,
				TimeCreated = timeNow,
				UserId = user.Id,
				RelUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyGoldFiat.Add(request);
			await DbContext.SaveChangesAsync();

			// activity
			var userActivity = CoreLogic.User.CreateUserActivity(
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"Gold buying request #{request.Id} confirmed",
				ip: agent.Ip,
				agent: agent.Agent,
				locale: GetUserLocale()
			);
			DbContext.UserActivity.Add(userActivity);
			await DbContext.SaveChangesAsync();

			// mark request for processing
			finHistory.Status = UserFinHistoryStatus.Manual;
			finHistory.RelUserActivityId = userActivity.Id;
			await DbContext.SaveChangesAsync();

			// don't wait KYC
			if (userTier >= UserTier.Tier2) {
				try {
					await OplogProvider.Update(request.OplogId, UserOpLogStatus.Pending, "Request confirmed by user");
				}
				catch { }

				// emission request
				NATS.Client.IConnection natsConnection = null;
				try {
					natsConnection = HttpContext.RequestServices.GetRequiredService<NATS.Client.IConnection>();

					var natsRequest = new Sumus.Sender.Send.Request() {
						RequestID = $"buy-{request.Id}",
						Amount = TextFormatter.FormatTokenAmount(estimation.ResultGoldAmount, Common.TokensPrecision.Sumus),
						Token = "GOLD",
						Wallet = model.SumusAddress,
					};

					var msg = await natsConnection.RequestAsync(Sumus.Sender.Send.Subject, Serializer.Serialize(natsRequest), 5000);
					var rep = Serializer.Deserialize<Sumus.Sender.Send.Reply>(msg.Data);
					if (!rep.Success) {
						throw new Exception(rep.Error);
					}

					Logger.Info($"{natsRequest.Amount} GOLD emission operation posted");
				} catch (Exception e) {
					Logger.Error(e, $"{TextFormatter.FormatTokenAmount(estimation.ResultGoldAmount, Common.TokensPrecision.Sumus)} GOLD emission operation failed to post");
					return APIResponse.GeneralInternalFailure(e);
				} finally {
					natsConnection?.Close();
				}
			}

			// update comment
			finHistory.Comment = $"Request #{request.Id}: { TextFormatter.FormatAmount(estimation.CentsPerGoldRate) } GOLD/{ exchangeCurrency.ToString().ToUpper() }";
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new CreditCardView() {
				}
			);
		}

	}
}
