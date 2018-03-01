using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.ExchangeModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class ExchangeController : BaseController {

		/// <summary>
		/// Selling request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/sell")]
		[ProducesResponseType(typeof(SellRequestView), 200)]
		public async Task<APIResponse> SellRequest([FromBody] SellRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var amountWei = BigInteger.Zero;
			if (!BigInteger.TryParse(model.Amount, out amountWei)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var currency = FiatCurrency.USD;
			var goldBalance = model.EthAddress == null ? BigInteger.Zero : await EthereumObserver.GetAddressGoldBalance(model.EthAddress);
			var mntpBalance = model.EthAddress == null ? BigInteger.Zero : await EthereumObserver.GetAddressMntpBalance(model.EthAddress);
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateSelling(
				goldAmountWei: amountWei,
				goldTotalVolumeWei: goldBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountWei < estimated.InputMin || amountWei > estimated.InputMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var ticket = await TicketDesk.NewGoldSelling(user, model.EthAddress, currency, estimated.InputUsed, goldRate, mntpBalance, estimated.ResultNetCents, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.GoldSell,
				AmountCents = estimated.ResultGrossCents,
				FeeCents = estimated.ResultFeeCents,
				Currency = currency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var sellRequest = new SellRequest() {
				User = user,
				Type = ExchangeRequestType.EthRequest,
				Status = ExchangeRequestStatus.Initial,
				Currency = currency,
				FiatAmountCents = estimated.ResultGrossCents,
				Address = model.EthAddress,
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,
				RefFinancialHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.SellRequest.Add(sellRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Selling order #{sellRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.InputUsed)} GOLD";
			await DbContext.SaveChangesAsync();
	
			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD selling request #{sellRequest.Id} ({TextFormatter.FormatAmount(estimated.ResultGrossCents, currency)}) from {Common.TextFormatter.MaskEthereumAddress(model.EthAddress)} initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new SellRequestView() {
					GoldAmount = estimated.InputUsed.ToString(),
					FiatAmount = estimated.ResultNetCents / 100d,
					FeeAmount = estimated.ResultFeeCents / 100d,
					GoldRate = goldRate / 100d,
					Payload = new[] { user.UserName, sellRequest.Id.ToString() },
				}
			);
		}

		/// <summary>
		/// Selling request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/sell")]
		[ProducesResponseType(typeof(HWSellRequestView), 200)]
		public async Task<APIResponse> HWSellRequest([FromBody] HWSellRequestModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var amountWei = BigInteger.Zero;
			if (!BigInteger.TryParse(model.Amount, out amountWei)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var currency = FiatCurrency.USD;
			var goldBalance = await EthereumObserver.GetUserGoldBalance(user.UserName);
			var mntpBalance = BigInteger.Zero;
			var goldRate = await GoldRateCached.GetGoldRate(currency);

			// estimate
			var estimated = await CoreLogic.Finance.Tokens.GoldToken.EstimateSelling(
				goldAmountWei: amountWei,
				goldTotalVolumeWei: goldBalance,
				pricePerGoldOunceCents: goldRate,
				mntpBalance: mntpBalance
			);

			// ---

			// invalid amount passed
			if (amountWei < estimated.InputMin || amountWei > estimated.InputMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// check rate
			var opLastTime = user.UserOptions.HotWalletSellingLastTime;
			if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
				// failed
				return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
			}

			var ticket = await TicketDesk.NewGoldSelling(user, null, currency, estimated.InputUsed, goldRate, mntpBalance, estimated.ResultNetCents, estimated.ResultFeeCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.GoldSell,
				AmountCents = estimated.ResultGrossCents,
				FeeCents = estimated.ResultFeeCents,
				Currency = currency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "" // see below
			};

			// add and save
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var sellRequest = new SellRequest() {
				User = user,
				Type = ExchangeRequestType.HWRequest,
				Status = ExchangeRequestStatus.Initial,
				Currency = currency,
				FiatAmountCents = estimated.ResultGrossCents,
				Address = "HW",
				FixedRateCents = goldRate,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow,
				RefFinancialHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.SellRequest.Add(sellRequest);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Selling order #{sellRequest.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(estimated.InputUsed)} GOLD";
			await DbContext.SaveChangesAsync();
	
			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Exchange,
				comment: $"GOLD selling request #{sellRequest.Id} ({TextFormatter.FormatAmount(estimated.ResultGrossCents, currency)}) from hot wallet initiated",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new HWSellRequestView() {
					GoldAmount = estimated.InputUsed.ToString(),
					FiatAmount = estimated.ResultNetCents / 100d,
					FeeAmount = estimated.ResultFeeCents / 100d,
					GoldRate = goldRate / 100d,
					RequestId = sellRequest.Id,
				}
			);
		}

	}
}