using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SwiftController : BaseController {

		/// <summary>
		/// Create swift withdraw request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("withdraw")]
		[ProducesResponseType(typeof(WithdrawView), 200)]
		public async Task<APIResponse> Withdraw([FromBody] WithdrawModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			// limits
			var transCurrency = FiatCurrency.USD;
			if (amountCents < AppConfig.Constants.SwiftData.WithdrawMin || amountCents > AppConfig.Constants.SwiftData.WithdrawMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// user
			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();
			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}
			if (!user.TwoFactorEnabled) {
				return APIResponse.BadRequest(APIErrorCode.AccountTFADisabled);
			}

			// actual user balance check
			var currentBalance = await EthereumObserver.GetUserFiatBalance(user.UserName, transCurrency);
			if (amountCents > currentBalance) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// new ticket
			var ticket = await TicketDesk.NewSwiftWithdraw(user, transCurrency, amountCents);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Status = FinancialHistoryStatus.Unconfirmed,
				Type = FinancialHistoryType.Withdraw,
				AmountCents = amountCents,
				FeeCents = 0,
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
				Comment = "", // see below
			};
			DbContext.FinancialHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// make payment
			var request = new DAL.Models.SwiftRequest() {
				Type = SwiftPaymentType.Withdraw,
				Status = SwiftPaymentStatus.Pending,
				Currency = transCurrency,
				AmountCents = amountCents,
				Holder = model.Holder.LimitLength(256),
				HolderAddress = "",
				Iban = model.Iban.LimitLength(256),
				Bank = model.Bank.LimitLength(256),
				Bic = model.Bic.LimitLength(128),
				Details = model.Details.LimitLength(1024),
				PaymentReference = "", // see below
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,

				RefFinancialHistoryId = finHistory.Id,
				UserId = user.Id,
			};
			finHistory.Status = FinancialHistoryStatus.Manual;
			DbContext.SwiftRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update
			request.PaymentReference = $"w-{user.UserName}-{request.Id}";
			finHistory.Comment = $"Swift withdraw request #{request.Id} ({request.PaymentReference})";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Swift,
				comment: $"Swift withdraw of {TextFormatter.FormatAmount(request.AmountCents, transCurrency)} requested ({request.PaymentReference})",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new WithdrawView() {
					Reference = request.PaymentReference,
				}
			);
		}
	}

}
