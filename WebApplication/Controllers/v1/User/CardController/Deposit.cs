using Goldmint.Common;
using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CardModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class CardController : BaseController {

		/// <summary>
		/// Deposit with card
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("deposit")]
		[ProducesResponseType(typeof(DepositView), 200)]
		public async Task<APIResponse> Deposit([FromBody] DepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userTier = CoreLogic.UserAccount.GetTier(user);
			var agent = GetUserAgentInfo();

			// ---

			// check pending operations
			if (HostingEnvironment.IsDevelopment() && await CoreLogic.UserAccount.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}
			// ---

			var transCurrency = FiatCurrency.USD;
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			if (amountCents < AppConfig.Constants.CardPaymentData.DepositMin || amountCents > AppConfig.Constants.CardPaymentData.DepositMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}
			
			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get card
			var card = user.Card.SingleOrDefault(
				c => c.Id == model.CardId && c.State == CardState.Verified
			);
			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Card not found");
			}

			// new ticket
			var ticket = await TicketDesk.NewCardDeposit(user, card, transCurrency, amountCents);

			// make payment
			var payment = await CardPaymentQueue.CreateDepositPayment(
				services: HttpContext.RequestServices,
				card: card,
				currency: transCurrency,
				amountCents: amountCents,
				deskTicketId: ticket
			);
			DbContext.CardPayment.Add(payment);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.Deposit,
				AmountCents = amountCents,
				FeeCents = 0,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Pending,
				TimeCreated = DateTime.UtcNow,
				User = user,
				Comment = "", // see below
			};
			DbContext.FinancialHistory.Add(finHistory);

			// save
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Deposit payment #{payment.Id} from {card.CardMask}";
			await DbContext.SaveChangesAsync();

			// own scope
			using (var scopedServices = HttpContext.RequestServices.CreateScope()) {

				// try
				var queryResult = await DepositQueue.StartDepositWithCard(
					services: scopedServices.ServiceProvider,
					userId: user.Id,
					userTier: userTier,
					payment: payment,
					financialHistoryId: finHistory.Id
				);

				// failed
				if (queryResult.Status != FiatEnqueueStatus.Success) {
					DbContext.FinancialHistory.Remove(finHistory);

					payment.Status = CardPaymentStatus.Cancelled;
					payment.ProviderStatus = "Cancelled";
					payment.ProviderMessage = $"Failed due to {queryResult.Status.ToString()}";
					payment.TimeCompleted = DateTime.UtcNow;

					await DbContext.SaveChangesAsync();

					try {
						await TicketDesk.UpdateTicket(ticket, UserOpLogStatus.Failed, payment.ProviderMessage);
					}
					catch {
					}

					if (queryResult.Error != null) {
						Logger.Error(queryResult.Error, $"Deposit #{payment.Id} attempt failed");
					}
				}

				switch (queryResult.Status) {

					case FiatEnqueueStatus.Success:

						// activity
						await CoreLogic.UserAccount.SaveActivity(
							services: scopedServices.ServiceProvider,
							user: user,
							type: Common.UserActivityType.CreditCard,
							comment:
							$"Deposit payment #{payment.Id} ({TextFormatter.FormatAmount(payment.AmountCents, transCurrency)}, card {card.CardMask}) initiated",
							ip: agent.Ip,
							agent: agent.Agent
						);

						return APIResponse.Success(
							new DepositView() {
							}
						);

					case FiatEnqueueStatus.Limit:
						return APIResponse.BadRequest(APIErrorCode.AccountDepositLimit);

					default:
						return APIResponse.BadRequest(APIErrorCode.AccountCardDepositFail, "Failed to charge deposit. Make sure you have enough money and card is valid, otherwise contact support");
				}
			}
		}
	}
}
