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
			if (amountCents < AppConfig.Constants.SwiftLimitsUSD.DepositMin || amountCents > AppConfig.Constants.SwiftLimitsUSD.DepositMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// user
			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();
			if (!CoreLogic.UserAccount.IsUserVerifiedL1(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			var transId = CardPaymentQueue.GenerateTransactionId();
			
			// var currentBalance = await EthereumObserver.GetUserFiatBalance(user.Id, transCurrency);

			// new ticket
			var ticket = await TicketDesk.CreateSwiftDepositTicket(TicketStatus.Opened, user.UserName, amountCents, transCurrency, "New swift deposit request");

			// make payment
			var request = new SwiftPayment() {
			};
			DbContext.CardPayment.Add(request);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.Deposit,
				AmountCents = amountCents,
				FeeCents = 0,
				Currency = transCurrency,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "", // see below
			};
			DbContext.FinancialHistory.Add(finHistory);

			// save
			await DbContext.SaveChangesAsync();
			DbContext.Detach(request, finHistory);

			// update comment
			finHistory.Comment = $"Direct deposit request #{request.Id}";
			DbContext.Update(finHistory);
			await DbContext.SaveChangesAsync();
			DbContext.Detach(finHistory);

			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Swift,
				comment: $"Direct deposit #{request.Id} ({TextFormatter.FormatAmount(request.AmountCents, transCurrency)} requested",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new DepositView() {
				}
			);
		}
	}
}
