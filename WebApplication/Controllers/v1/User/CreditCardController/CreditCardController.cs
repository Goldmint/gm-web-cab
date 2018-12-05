using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Finance;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.User.CreditCardModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Goldmint.WebApplication.Models.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User.CreditCardController {

	[Route("api/v1/user/ccard")]
	public partial class CreditCardController : BaseController {

		/// <summary>
		/// Cards list
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List() {

			var user = await GetUserFromDb();

			var cards = await (
				from c in DbContext.UserCreditCard
				where 
					c.UserId == user.Id &&
					c.State != CardState.InputDepositData &&
					c.State != CardState.Deleted
				select c
			)
				.AsNoTracking()
				.ToListAsync()
			;

			return APIResponse.Success(
				new ListView() {
					List = cards.Select(c => new ListView.Item() {
						CardId = c.Id,
						Mask = c.CardMask,
						Status = GetCardStatus(c),
					}).ToArray()
				}
			);
		}

		/// <summary>
		/// Add card
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("add")]
		[ProducesResponseType(typeof(AddView), 200)]
		public async Task<APIResponse> Add([FromBody] AddModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

		    // ---
		    var rcfg = RuntimeConfigHolder.Clone();

            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			var oneDollarVerification = HostingEnvironment.IsDevelopment() || HostingEnvironment.IsStaging();

			// verification payment
			var verificationAmountCents = 100L + (SecureRandom.GetPositiveInt() % 100);
			if (oneDollarVerification) {
				verificationAmountCents = 100L;
			}

			// new unverified card
			var card = new DAL.Models.UserCreditCard() {
				State = CardState.InputDepositData,
				User = user,
				VerificationAmountCents = verificationAmountCents,
				TimeCreated = DateTime.UtcNow,
			};
			DbContext.UserCreditCard.Add(card);
			await DbContext.SaveChangesAsync();

			// replace card id in redirect
			model.Redirect = model.Redirect?.Replace(":cardId", card.Id.ToString(), StringComparison.InvariantCultureIgnoreCase);
			if (!Common.ValidationRules.BeValidUrl(model.Redirect)) {
				return APIResponse.BadRequest(nameof(model.Redirect), "Invalid format");
			}

			// new gw transaction
			var transId = CoreLogic.Finance.The1StPaymentsProcessing.GenerateTransactionId();
			var transCurrency = FiatCurrency.Usd;

			var transData = new CoreLogic.Services.The1StPayments.StartPaymentCardStore3D() {

				RedirectUrl = model.Redirect,

				TransactionId = transId,
			    AmountCents = 100,
                Currency = transCurrency,
				Purpose = "Card data for deposit payments at goldmint.io",

				SenderName = user.UserVerification.FirstName + " " + user.UserVerification.LastName,
				SenderEmail = user.Email,
				SenderPhone = user.UserVerification.PhoneNumber,
				SenderIP = agent.IpObject,

				SenderAddressCountry = user.UserVerification.CountryCode,
				SenderAddressState = user.UserVerification.State?.Limit(20),
				SenderAddressCity = user.UserVerification.City?.Limit(25),
				SenderAddressStreet = user.UserVerification.Street?.Limit(50),
				SenderAddressZip = user.UserVerification.PostalCode?.Limit(15),
			};

			// get redirect
			var paymentResult = await The1StPayments.StartPaymentCardStore3D(transData);
			if (paymentResult.Redirect == null) {
				throw new Exception("Redirect is null");
			}

			// save transaction
			card.GwInitialDepositCardTransactionId = paymentResult.GWTransactionId;
			await DbContext.SaveChangesAsync();

			// make ticket
			var ticketId = await OplogProvider.NewCardVerification(user.Id, card.Id, card.VerificationAmountCents, transCurrency);

			// enqueue payment
			var payment = The1StPaymentsProcessing.CreateCardDataInputPayment(
				card: card,
				type: CardPaymentType.CardDataInputSMS,
				transactionId: transId,
				gwTransactionId: paymentResult.GWTransactionId,
				oplogId: ticketId,
				amountCents: 100
			);
			payment.Status = CardPaymentStatus.Pending;
			DbContext.CreditCardPayment.Add(payment);
			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(ticketId, UserOpLogStatus.Pending, $"Payment for first step is #{payment.Id}");
			}
			catch { }

			return APIResponse.Success(
				new AddView() {
					CardId = card.Id,
					Redirect = paymentResult.Redirect,
				}
			);
		}

		/*
		/// <summary>
		/// Confirm card
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(ConfirmView), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// ---

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			// get the card
			var card = await (
					from c in DbContext.UserCreditCard
					where
						c.UserId == user.Id &&
						c.Id == model.CardId &&
						c.State == CardState.InputWithdrawData &&
						c.GwInitialDepositCardTransactionId != null
					select c
				)
				.AsTracking()
				.FirstOrDefaultAsync()
			;
			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
			}

			// find first data input operation
			var prevPayment = await (
				from p in DbContext.CreditCardPayment
				where
					p.UserId == user.Id &&
					p.CardId == card.Id &&
					p.Type == CardPaymentType.CardDataInputSMS &&
					p.Status == CardPaymentStatus.Success &&
					p.GwTransactionId == card.GwInitialDepositCardTransactionId
				select p
			)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;
			if (prevPayment == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
			}

			// replace card id in redirect
			model.Redirect = model.Redirect?.Replace(":cardId", card.Id.ToString(), StringComparison.InvariantCultureIgnoreCase);
			if (!Common.ValidationRules.BeValidUrl(model.Redirect)) {
				return APIResponse.BadRequest(nameof(model.Redirect), "Invalid format");
			}

			// new gw transaction
			var transId = The1StPaymentsProcessing.GenerateTransactionId();
			var transCurrency = FiatCurrency.Usd;

			var transData = new CoreLogic.Services.The1StPayments.StartCreditCardStore() {

				RedirectUrl = model.Redirect,

				TransactionId = transId,
				AmountCents = 100,
				Currency = transCurrency,
				Purpose = "Card data for withdrawal payments at goldmint.io",

				RecipientName = user.UserVerification.FirstName + " " + user.UserVerification.LastName,
				RecipientEmail = user.Email,
				RecipientPhone = user.UserVerification.PhoneNumber,
				RecipientIP = agent.IpObject,

				RecipientAddressCountry = user.UserVerification.CountryCode,
				RecipientAddressState = user.UserVerification.State,
				RecipientAddressCity = user.UserVerification.City,
				RecipientAddressStreet = user.UserVerification.Street,
				RecipientAddressZip = user.UserVerification.PostalCode,
			};

			// get redirect
			var paymentResult = await The1StPayments.StartCreditCardStore(transData);
			if (paymentResult.Redirect == null) {
				throw new Exception("Redirect is null");
			}

			// update card
			card.GwInitialWithdrawCardTransactionId = paymentResult.GWTransactionId;
			await DbContext.SaveChangesAsync();

			// enqueue payment
			var payment = The1StPaymentsProcessing.CreateCardDataInputPayment(
				card: card,
				type: CardPaymentType.CardDataInputCRD,
				transactionId: transId,
				gwTransactionId: paymentResult.GWTransactionId,
				oplogId: prevPayment.OplogId
			);
			payment.Status = CardPaymentStatus.Pending;
			DbContext.CreditCardPayment.Add(payment);
			await DbContext.SaveChangesAsync();

			try {
				await OplogProvider.Update(prevPayment.OplogId, UserOpLogStatus.Pending, $"Payment for second step is {payment.Id}");
			}
			catch { }

			return APIResponse.Success(
				new ConfirmView() {
					Redirect = paymentResult.Redirect,
				}
			);
		}
		*/

		/// <summary>
		/// Verify card by security code
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("verify")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Verify([FromBody] VerifyModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// get code digits
			model.Code = string.Join("", model.Code.Where(char.IsDigit).Select(_ => _.ToString()).ToArray());

            // ---
		    var rcfg = RuntimeConfigHolder.Clone();
            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);
			var userLocale = GetUserLocale();
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			// get the card
			var card = await (
					from c in DbContext.UserCreditCard
					where
						c.UserId == user.Id &&
						c.Id == model.CardId &&
						c.State == CardState.Verification
					select c
				)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (card != null && card.VerificationAmountCents > 0) {

				// code matches
				if (card.VerificationAmountCents.ToString().ToUpper() == model.Code.ToUpper()) {

					card.State = CardState.Verified;
					
					// activity
					var userActivity = CoreLogic.User.CreateUserActivity(
						user: user,
						type: Common.UserActivityType.CreditCard,
						comment: $"Card { card.CardMask } verified",
						ip: agent.Ip,
						agent: agent.Agent,
						locale: userLocale
					);
					DbContext.UserActivity.Add(userActivity);
					await DbContext.SaveChangesAsync();

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

			return APIResponse.BadRequest(nameof(model.Code), "Invalid format");
		}

		/// <summary>
		/// Card status
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("status")]
		[ProducesResponseType(typeof(StatusView), 200)]
		public async Task<APIResponse> Status([FromBody] StatusModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

		    // ---
		    var rcfg = RuntimeConfigHolder.Clone();
            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			// get the card
			var card = await (
					from c in DbContext.UserCreditCard
					where
						c.UserId == user.Id &&
						c.Id == model.CardId
					select c
				)
				.AsTracking()
				.FirstOrDefaultAsync()
			;
			if (card == null) {
				return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
			}

			// for first two steps we need to check transaction on acquirer side
			string gwTid = null;

			// step 1 - deposit data
			if (card.State == CardState.InputDepositData) {
				gwTid = card.GwInitialDepositCardTransactionId;
			}
			// step 2 - withdrawal data
			if (card.State == CardState.InputWithdrawData) {
				gwTid = card.GwInitialWithdrawCardTransactionId;
			}

			if (gwTid != null) {

				// get payment by transaction id
				var payment = await (
					from p in DbContext.CreditCardPayment
					where
					p.GwTransactionId == gwTid &&
					p.UserId == user.Id
					select p
				)
					.AsNoTracking()
					.SingleOrDefaultAsync()
				;

				if (payment == null) {
					return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
				}

				// check payment + update card status
				if (payment.Status == Common.CardPaymentStatus.Pending) {

					// own scope
					using (var scopedServices = HttpContext.RequestServices.CreateScope()) {

						var presult = await The1StPaymentsProcessing.ProcessPendingCardDataInputPayment(
							scopedServices.ServiceProvider,
							payment.Id
						);

						// just charged - try to check payment
						if (presult.VerificationPaymentId != null) {

							// own scope
							using (var scopedServices2 = HttpContext.RequestServices.CreateScope()) {
								await The1StPaymentsProcessing.ProcessVerificationPayment(
									scopedServices2.ServiceProvider,
									presult.VerificationPaymentId.Value
								);
							}
						}
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

		/// <summary>
		/// Remove card by ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("remove")]
		[ProducesResponseType(typeof(RemoveView), 200)]
		public async Task<APIResponse> Remove([FromBody] RemoveModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// ---

			var user = await GetUserFromDb();
			var userLocale = GetUserLocale();
			var agent = GetUserAgentInfo();

			// ---

			// get the card
			var card = await (
					from c in DbContext.UserCreditCard
					where
						c.UserId == user.Id &&
						c.Id == model.CardId &&
						(
							c.State == CardState.Verification || 
							c.State == CardState.Verified
						)
					select c
				)
				.AsTracking()
				.FirstOrDefaultAsync()
			;
			if (card != null) {

				card.State = CardState.Deleted;

				if (await DbContext.SaveChangesAsync() > 0) {

					// activity
					var userActivity = CoreLogic.User.CreateUserActivity(
						user: user,
						type: Common.UserActivityType.CreditCard,
						comment: $"Card {card.CardMask} removed",
						ip: agent.Ip,
						agent: agent.Agent,
						locale: userLocale
					);
					DbContext.UserActivity.Add(userActivity);
					await DbContext.SaveChangesAsync();
				}

				return APIResponse.Success(
					new RemoveView()
				);
			}

			return APIResponse.BadRequest(nameof(model.CardId), "Invalid id");
		}

		// ---

		[NonAction]
		private string GetCardStatus(DAL.Models.UserCreditCard card) {
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