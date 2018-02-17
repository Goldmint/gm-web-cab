using Goldmint.Common;
using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CardModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/fiat/card")]
	public partial class CardController : BaseController {

		/// <summary>
		/// Cards list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpGet, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List() {

			var user = await GetUserFromDb();
			var cards =
				from c in user.Card
				where c.State != CardState.InputDepositData && c.State != CardState.Deleted
				select c
			;

			var list = new List<ListView.Item>();
			foreach (var c in cards) {
				list.Add(new ListView.Item() {
					CardId = c.Id,
					Mask = c.CardMask,
					Status = GetCardStatus(c),
				});
			}

			return APIResponse.Success(
				new ListView() {
					List = list.ToArray(),
				}
			);
		}

		/// <summary>
		/// Add card
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("add")]
		[ProducesResponseType(typeof(AddView), 200)]
		public async Task<APIResponse> Add([FromBody] AddModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// verification code
			var verificationAmountCents = 100L + (SecureRandom.GetPositiveInt() % 100);
			if (!HostingEnvironment.IsProduction()) {
				verificationAmountCents = 100L;
			}

			// new unverified card
			var card = new DAL.Models.Card() {
				State = CardState.InputDepositData,
				User = user,
				VerificationAmountCents = verificationAmountCents,
				TimeCreated = DateTime.UtcNow,
			};
			DbContext.Card.Add(card);
			DbContext.SaveChanges();

			// replace card id in redirect
			model.Redirect = model.Redirect?.Replace(":cardId", card.Id.ToString(), StringComparison.InvariantCultureIgnoreCase);
			if (!Common.ValidationRules.BeValidDectaRedirectUrl(model.Redirect)) {
				return APIResponse.BadRequest(nameof(model.Redirect), "Invalid redirect url");
			}

			// new gw transaction
			var transId = CardPaymentQueue.GenerateTransactionId();
			var transCurrency = FiatCurrency.USD;

			var transData = new CoreLogic.Services.Acquiring.StartPaymentCardStore() {

				RedirectUrl = model.Redirect,

				TransactionId = transId,
				AmountCents = 100,
				Currency = transCurrency,
				Purpose = "Card data for deposit payments at goldmint.io",

				SenderName = user.UserVerification.FirstName + " " + user.UserVerification.LastName,
				SenderEmail = user.Email,
				SenderPhone = user.UserVerification.PhoneNumber,
				SenderIP = agent.IpObject,

				SenderAddressCountry = new System.Globalization.RegionInfo(user.UserVerification.CountryCode),
				SenderAddressState = user.UserVerification.State,
				SenderAddressCity = user.UserVerification.City,
				SenderAddressStreet = user.UserVerification.Street,
				SenderAddressZip = user.UserVerification.PostalCode,
			};

			// get redirect
			var paymentResult = await CardAcquirer.StartPaymentCardStore(transData);
			if (paymentResult.Redirect == null) {
				throw new Exception("Redirect is null");
			}

			// save transaction
			card.GWInitialDepositCardTransactionId = paymentResult.GWTransactionId;
			DbContext.Update(card);
			DbContext.SaveChanges();

			// make ticket
			var ticketId = await TicketDesk.NewCardVerification(user, card);

			// enqueue payment
			var payment = CardPaymentQueue.CreateCardDataInputPayment(
				card: card,
				type: CardPaymentType.CardDataInputSMS,
				transactionId: transId,
				gwTransactionId: paymentResult.GWTransactionId,
				deskTicketId: ticketId
			);
			DbContext.CardPayment.Add(payment);
			DbContext.SaveChanges();
			DbContext.Detach(payment);

			try {
				await TicketDesk.UpdateTicket(ticketId, UserOpLogStatus.Pending, $"Payment for first step is #{payment.Id}");
			}
			catch { }

			return APIResponse.Success(
				new AddView() {
					CardId = card.Id,
					Redirect = paymentResult.Redirect,
				}
			);
		}

		/// <summary>
		/// Confirm card
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(ConfirmView), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get the card
			var card = user.Card.FirstOrDefault(
				c => c.Id == model.CardId &&
				c.State == CardState.InputWithdrawData &&
				c.GWInitialDepositCardTransactionId != null
			);

			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Card not found");
			}

			// find first data input operation
			var prevPayment = await (
				from p in DbContext.CardPayment
				where
				p.UserId == user.Id &&
				p.Type == CardPaymentType.CardDataInputSMS &&
				p.Status == CardPaymentStatus.Success &&
				p.GWTransactionId == card.GWInitialDepositCardTransactionId
				select p
			)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;
			if (prevPayment == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Card payment not found");
			}

			// replace card id in redirect
			model.Redirect = model.Redirect?.Replace(":cardId", card.Id.ToString(), StringComparison.InvariantCultureIgnoreCase);
			if (!Common.ValidationRules.BeValidDectaRedirectUrl(model.Redirect)) {
				return APIResponse.BadRequest(nameof(model.Redirect), "Invalid redirect url");
			}

			// new gw transaction
			var transId = CardPaymentQueue.GenerateTransactionId();
			var transCurrency = FiatCurrency.USD;

			var transData = new CoreLogic.Services.Acquiring.StartCreditCardStore() {

				RedirectUrl = model.Redirect,

				TransactionId = transId,
				AmountCents = 100,
				Currency = transCurrency,
				Purpose = "Card data for withdrawal payments at goldmint.io",

				RecipientName = user.UserVerification.FirstName + " " + user.UserVerification.LastName,
				RecipientEmail = user.Email,
				RecipientPhone = user.UserVerification.PhoneNumber,
				RecipientIP = agent.IpObject,

				RecipientAddressCountry = new System.Globalization.RegionInfo(user.UserVerification.CountryCode),
				RecipientAddressState = user.UserVerification.State,
				RecipientAddressCity = user.UserVerification.City,
				RecipientAddressStreet = user.UserVerification.Street,
				RecipientAddressZip = user.UserVerification.PostalCode,
			};

			// get redirect
			var paymentResult = await CardAcquirer.StartCreditCardStore(transData);
			if (paymentResult.Redirect == null) {
				throw new Exception("Redirect is null");
			}

			// update card
			card.GWInitialWithdrawCardTransactionId = paymentResult.GWTransactionId;
			DbContext.Update(card);
			DbContext.SaveChanges();

			// enqueue payment
			var payment = CardPaymentQueue.CreateCardDataInputPayment(
				card: card,
				type: CardPaymentType.CardDataInputCRD,
				transactionId: transId,
				gwTransactionId: paymentResult.GWTransactionId,
				deskTicketId: prevPayment.DeskTicketId
			);
			DbContext.CardPayment.Add(payment);
			DbContext.SaveChanges();
			DbContext.Detach(payment);

			try {
				await TicketDesk.UpdateTicket(prevPayment.DeskTicketId, UserOpLogStatus.Pending, $"Payment for second step is {payment.Id}");
			}
			catch { }

			return APIResponse.Success(
				new AddView() {
					Redirect = paymentResult.Redirect,
				}
			);
		}

		/// <summary>
		/// Verify card by security code
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("verify")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Verify([FromBody] VerifyModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get the card
			var card = user.Card.FirstOrDefault(
				c => c.Id == model.CardId &&
				c.State == CardState.Verification
			);

			// get code digits
			model.Code = string.Join("", model.Code.Where(c => char.IsDigit(c)).Select(c => c.ToString()).ToArray());

			if (card != null && card.VerificationAmountCents > 0) {

				// code matches
				if (card.VerificationAmountCents.ToString().ToUpper() == model.Code.ToUpper()) {

					card.State = CardState.Verified;
					DbContext.SaveChanges();

					// activity
					await CoreLogic.UserAccount.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.CreditCard,
						comment: $"Card {card.CardMask} verified",
						ip: agent.Ip,
						agent: agent.Agent
					);

					return APIResponse.Success();
				}
				else {
					card.VerificationAttempt++;
					if (card.VerificationAttempt >= 3) {
						card.State = CardState.Deleted;
					}
					DbContext.SaveChanges();
				}
			}

			return APIResponse.BadRequest(nameof(model.Code), "Invalid code");
		}

		/// <summary>
		/// Card status
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("status")]
		[ProducesResponseType(typeof(StatusView), 200)]
		public async Task<APIResponse> Status([FromBody] StatusModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!CoreLogic.UserAccount.IsUserVerifiedL0(user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get the card
			var card = user.Card.FirstOrDefault(
				c => c.Id == model.CardId
			);

			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Card not found");
			}

			// for first two steps we need to check transaction on acquirer side
			string gwTid = null;

			// step 1 - deposit data
			if (card.State == CardState.InputDepositData) {
				gwTid = card.GWInitialDepositCardTransactionId;
			}
			// step 2 - withdrawal data
			if (card.State == CardState.InputWithdrawData) {
				gwTid = card.GWInitialWithdrawCardTransactionId;
			}

			if (gwTid != null) {

				// get payment by transaction id
				var payment = await (
					from p in DbContext.CardPayment
					where
					p.GWTransactionId == gwTid &&
					p.UserId == user.Id
					select p
				)
					.AsNoTracking()
					.SingleOrDefaultAsync()
				;

				if (payment == null) {
					return APIResponse.BadRequest(nameof(model.CardId), "Card tx not found");
				}

				// check payment + update card status
				if (payment.Status == Common.CardPaymentStatus.Pending) {
					var presult = await CardPaymentQueue.ProcessPendingCardDataInputPayment(HttpContext.RequestServices, payment.Id);

					// just charged - try to check payment
					if (presult.VerificationPaymentId != null) {
						await CardPaymentQueue.ProcessVerificationPayment(HttpContext.RequestServices, presult.VerificationPaymentId.Value);
					}
				}

				// reload card for sure
				await DbContext.Entry(card).ReloadAsync();
			}

			return APIResponse.Success(
				new StatusView() {
					Status = GetCardStatus(card),
				}
			);
		}

		// ---

		[NonAction]
		private string GetCardStatus(Card card) {
			switch (card.State) {
				case CardState.InputDepositData: return "initial";
				case CardState.InputWithdrawData: return "confirm";
				case CardState.Payment: return "payment";
				case CardState.Verification: return "verification";
				case CardState.Verified: return "verified";
				case CardState.Disabled: return "disabled";
			}
			return "failed";
		}
	}
}
