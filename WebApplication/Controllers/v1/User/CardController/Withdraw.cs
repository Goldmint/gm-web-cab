﻿using Goldmint.Common;
using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CardModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class CardController : BaseController {

		/// <summary>
		/// Withdraw to the card
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("withdraw")]
		[ProducesResponseType(typeof(WithdrawView), 200)]
		public async Task<APIResponse> Withdraw([FromBody] WithdrawModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var transCurrency = FiatCurrency.USD;
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			if (amountCents < AppConfig.Constants.CardPaymentData.WithdrawMin || amountCents > AppConfig.Constants.CardPaymentData.WithdrawMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			if (!user.TwoFactorEnabled) {
				return APIResponse.BadRequest(APIErrorCode.AccountTFADisabled);
			}

			// get card
			var card = user.Card.SingleOrDefault(
				c => c.Id == model.CardId && c.State == CardState.Verified
			);
			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Card not found");
			}

			// new ticket
			var ticket = await TicketDesk.NewCardWithdraw(user, card, transCurrency, amountCents);

			// make payment
			var payment = await CardPaymentQueue.CreateWithdrawPayment(
				services: HttpContext.RequestServices,
				card: card,
				currency: transCurrency,
				amountCents: amountCents,
				deskTicketId: ticket
			);
			DbContext.CardPayment.Add(payment);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.Withdraw,
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
			DbContext.SaveChanges();
			DbContext.Detach(payment, finHistory);

			// update comment
			finHistory.Comment = $"Withdrawal payment #{payment.Id} to {card.CardMask}";
			DbContext.Update(finHistory);
			await DbContext.SaveChangesAsync();
			DbContext.Detach(finHistory);

			// try
			var queryResult = await WithdrawQueue.StartWithdrawWithCard(
				services: HttpContext.RequestServices,
				payment: payment,
				financialHistory: finHistory
			);

			switch (queryResult.Status) {

				case FiatEnqueueStatus.Success:

					// activity
					await CoreLogic.UserAccount.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.CreditCard,
						comment: $"Withdrawal payment #{payment.Id} ({TextFormatter.FormatAmount(payment.AmountCents, transCurrency)}, card ) initiated",
						ip: agent.Ip,
						agent: agent.Agent
					);

					return APIResponse.Success(
						new WithdrawView() {
						}
					);

				case FiatEnqueueStatus.Limit:
					return APIResponse.BadRequest(APIErrorCode.AccountWithdrawLimit);

				default:
					return APIResponse.BadRequest(APIErrorCode.AccountCardWithdrawFail, "Failed to make withdraw. Make sure card is valid, otherwise contact support");
			}
		}
	}
}