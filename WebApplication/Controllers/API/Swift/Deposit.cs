using Goldmint.Common;
using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	public partial class SwiftController : BaseController {

		/// <summary>
		/// Create swift deposit request
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("deposit")]
		[ProducesResponseType(typeof(DepositView), 200)]
		public async Task<APIResponse> Deposit([FromBody] DepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			// limits
			var transCurrency = FiatCurrency.USD;
			if (amountCents < AppConfig.Constants.SwiftData.DepositMin || amountCents > AppConfig.Constants.SwiftData.DepositMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// user
			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();
			if (!CoreLogic.UserAccount.IsUserVerifiedL1(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// new ticket
			var ticket = await TicketDesk.CreateSwiftDepositTicket(TicketStatus.Opened, user.UserName, amountCents, transCurrency, "New swift deposit request");

			// make payment
			var request = new DAL.Models.SwiftPayment() {
				Type = SwiftPaymentType.Deposit,
				Status = SwiftPaymentStatus.Pending,
				Currency = transCurrency,
				AmountCents = amountCents,
				BenName = AppConfig.Constants.SwiftData.BenName,
				BenAddress = AppConfig.Constants.SwiftData.BenAddress,
				BenIban = AppConfig.Constants.SwiftData.BenIban,
				BenBankName = AppConfig.Constants.SwiftData.BenBankName,
				BenBankAddress = AppConfig.Constants.SwiftData.BenBankAddress,
				BenSwift = AppConfig.Constants.SwiftData.BenSwift,
				PaymentReference = "", // see below
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
			};
			DbContext.SwiftPayment.Add(request);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.Deposit,
				AmountCents = amountCents,
				FeeCents = 0,
				Currency = transCurrency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
				Comment = "", // see below
			};
			DbContext.FinancialHistory.Add(finHistory);

			// save
			await DbContext.SaveChangesAsync();

			// update
			request.PaymentReference = $"Order number: {request.Id}";
			finHistory.Comment = $"Swift deposit request #{request.Id}";

			await DbContext.SaveChangesAsync();
			DbContext.Detach(request, finHistory);

			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Swift,
				comment: $"Swift deposit #{request.Id} ({TextFormatter.FormatAmount(request.AmountCents, transCurrency)} requested",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new DepositView() {
					BenName = request.BenName,
					BenAddress = request.BenAddress,
					BenIban = request.BenIban,
					BenBankName = request.BenBankName,
					BenBankAddress = request.BenBankAddress,
					BenSwift = request.BenSwift,
					Reference = request.PaymentReference,
				}
			);
		}
	}
}
