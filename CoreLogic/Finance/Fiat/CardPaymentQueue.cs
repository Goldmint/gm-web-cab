using Goldmint.Common;
using Goldmint.CoreLogic.Services.Acquiring;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance.Fiat {

	public static class CardPaymentQueue {

		/// <summary>
		/// New card input data operation to enqueue
		/// </summary>
		public static CardPayment CreateCardDataInputPayment(Card card, CardPaymentType type, string transactionId, string gwTransactionId, string deskTicketId) {

			if (card.User == null) throw new ArgumentException("User not included");

			// new deposit payment
			return new CardPayment() {
				Card = card,
				TransactionId = transactionId,
				GWTransactionId = gwTransactionId,
				Type = type,
				User = card.User,
				Currency = FiatCurrency.USD,
				AmountCents = 0,
				Status = CardPaymentStatus.Pending,
				DeskTicketId = deskTicketId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(15 * 60),
			};
		}

		/// <summary>
		/// New verification payment
		/// </summary>
		public static async Task<CardPayment> CreateVerificationPayment(IServiceProvider services, Card card, string deskTicketId) {

			if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();

			// ---

			var amountCents = card.VerificationAmountCents;
			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartPaymentCharge(new StartPaymentCharge() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GWInitialDepositCardTransactionId,
				Purpose = "Card verification at goldmint.io"
			});

			return new CardPayment() {
				Card = card,
				TransactionId = tid,
				GWTransactionId = gwTransactionId,
				Type = CardPaymentType.Verification,
				User = card.User,
				Currency = FiatCurrency.USD,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Pending,
				DeskTicketId = deskTicketId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		/// <summary>
		/// New deposit payment
		/// </summary>
		public static async Task<CardPayment> CreateDepositPayment(IServiceProvider services, Card card, FiatCurrency currency, long amountCents, string deskTicketId) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (card.State != CardState.Verified) throw new ArgumentException("Card not verified");
			if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();

			// ---

			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartPaymentCharge(new StartPaymentCharge() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GWInitialDepositCardTransactionId,
				Purpose = "Deposit at goldmint.io",
				DynamicDescriptor = null,
			});

			return new CardPayment() {
				Card = card,
				TransactionId = tid,
				GWTransactionId = gwTransactionId,
				Type = CardPaymentType.Deposit,
				User = card.User,
				Currency = currency,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Pending,
				DeskTicketId = deskTicketId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		/// <summary>
		/// New payment refund
		/// </summary>
		public static CardPayment CreateRefundPayment(CardPayment refPayment, string deskTicketId) {

			if (refPayment.Type != CardPaymentType.Deposit && refPayment.Type != CardPaymentType.Verification) {
				throw new ArgumentException("Ref payment must be of deposit or verification type");
			}
			if (refPayment.Status != CardPaymentStatus.Success) throw new ArgumentException("Cant refund unsuccessful payment");
			if (refPayment.Card == null) throw new ArgumentException("Card not included");
			if (refPayment.User == null) throw new ArgumentException("User not included");

			// new refund payment
			return new CardPayment() {
				Card = refPayment.Card,
				TransactionId = GenerateTransactionId(),
				GWTransactionId = "", // empty until charge
				RefPayment = refPayment,
				Type = CardPaymentType.Refund,
				User = refPayment.User,
				Currency = refPayment.Currency,
				AmountCents = refPayment.AmountCents,
				Status = CardPaymentStatus.Pending,
				DeskTicketId = deskTicketId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(15 * 60),
			};
		}

		/// <summary>
		/// New withdraw payment
		/// </summary>
		public static async Task<CardPayment> CreateWithdrawPayment(IServiceProvider services, Card card, FiatCurrency currency, long amountCents, string deskTicketId) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (card.State != CardState.Verified) throw new ArgumentException("Card not verified");
			if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();

			// ---

			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartCreditCharge(new StartCreditCharge() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GWInitialWithdrawCardTransactionId,
				Purpose = "Withdraw at goldmint.io",
				DynamicDescriptor = null,
			});

			return new CardPayment() {
				Card = card,
				TransactionId = tid,
				GWTransactionId = gwTransactionId,
				Type = CardPaymentType.Withdraw,
				User = card.User,
				Currency = currency,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Pending,
				DeskTicketId = deskTicketId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		// ---

		/// <summary>
		/// Checks card data input transaction (actually this is not payment)
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static async Task<ProcessPendingCardDataInputPaymentResult> ProcessPendingCardDataInputPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(CardPaymentQueue));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();
			var hostingEnv = services.GetRequiredService<IHostingEnvironment>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			// pending by default
			var ret = new ProcessPendingCardDataInputPaymentResult() {
				Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.Pending,
				VerificationPaymentId = null,
			};

			return await mutexBuilder.LockAsync<ProcessPendingCardDataInputPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CardPayment
						where
						p.Id == paymentId &&
						(p.Type == CardPaymentType.CardDataInputSMS || p.Type == CardPaymentType.CardDataInputCRD || p.Type == CardPaymentType.CardDataInputP2P) &&
						p.Status == CardPaymentStatus.Pending
						select p
					)
					.Include(p => p.Card)
					.Include(p => p.User).ThenInclude(u => u.UserVerification)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					// not found
					if (payment == null) {
						ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.NothingToDo;
						return ret;
					}

					// update payment
					bool finalized = false;
					string cardHolder = null;
					string cardMask = null;
					{

						// query acquirer
						var result = await cardAcquirer.CheckCardStored(payment.GWTransactionId);

						// set new status
						switch (result.Status) {

							case CardGatewayTransactionStatus.Success:
								payment.Status = CardPaymentStatus.Success; // skip confirmed state
								finalized = true;
								break;

							case CardGatewayTransactionStatus.Failed:
							case CardGatewayTransactionStatus.NotFound:
								payment.Status = CardPaymentStatus.Failed;
								finalized = true;
								break;

							default:
								payment.Status = CardPaymentStatus.Pending;
								break;
						}

						// set additinal fields
						payment.ProviderStatus = result.ProviderStatus;
						payment.ProviderMessage = result.ProviderMessage;
						payment.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(payment.TimeCreated, TimeSpan.FromSeconds(15 * 60), 1);

						// get card data if possible
						if (result.CardHolder != null) {
							cardHolder = result.CardHolder;
							cardMask = result.CardMask;
						}

						// finalize
						if (finalized) {
							payment.TimeCompleted = DateTime.UtcNow;
						}
					}

					// update payment
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();
					dbContext.Detach(payment);

					// now is final
					if (finalized) {

						CardPayment verificationPaymentEnqueued = null;
						var card = payment.Card;
						
						// delete card on any data mismatch
						var cardPrevState = card.State;
						card.State = CardState.Deleted;

						if (payment.Status == CardPaymentStatus.Success) {

							// set next step
							if (cardHolder != null &&
								cardMask != null &&
								UserAccount.IsUserVerifiedL0(payment.User) &&
								cardHolder.Contains(payment.User.UserVerification.FirstName) &&
								cardHolder.Contains(payment.User.UserVerification.LastName)
							) {

								// this is 1st step - deposit data
								if (cardPrevState == CardState.InputDepositData) {

									card.State = CardState.InputWithdrawData;
									card.CardHolder = cardHolder;
									card.CardMask = cardMask;
									
									try {
										await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, "Provided card data on first step is saved");
									}
									catch { }

									// ok
									ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.DepositSuccess;
								}

								// this is 2nd step - withdraw data - must be the same card
								else if (cardPrevState == CardState.InputWithdrawData && card.CardMask != null) {

									// mask matched
									if (card.CardMask == cardMask || !(hostingEnv?.IsProduction() ?? true)) {

										card.State = CardState.Payment;

										try {
											await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, "Provided card data on second step is saved");
										}
										catch { }

										// enqueue verification payment
										try {
											var verPayment = await CreateVerificationPayment(
												services: services,
												card: card,
												deskTicketId: payment.DeskTicketId
											);
											dbContext.CardPayment.Add(verPayment);
											verificationPaymentEnqueued = verPayment;

											// ok, for now
											ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.WithdrawSuccess;
										}
										catch (Exception e) {
											logger?.Error(e, $"Failed to start verification charge for this payment");

											// failed to charge
											ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.FailedToChargeVerification;
										}
									}
									else {
										// mask mismatched
										ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.WithdrawCardDataMismatched;

										await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Failed, "Provided card data is mismatched");
									}
								}
							}
						}
						else if (payment.Status == CardPaymentStatus.Failed) {
							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Failed, $"Card data input step is unsuccessful on a gateway side");
						}

						// update card state
						dbContext.Update(card);
						await dbContext.SaveChangesAsync();
						dbContext.Detach(card, payment);

						if (verificationPaymentEnqueued != null) {
							// saved, dont track instance
							dbContext.Detach(verificationPaymentEnqueued);

							ret.VerificationPaymentId = verificationPaymentEnqueued.Id;

							try {
								await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, $"Verification payment #{verificationPaymentEnqueued.Id} enqueued");
							}
							catch { }
						}
					}
				}

				return ret;
			});
		}

		/// <summary>
		/// Checks verification card payment vs acquirer with exclusive write access on this payment
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static async Task<ProcessVerificationPaymentResult> ProcessVerificationPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(CardPaymentQueue));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			// by default
			var ret = new ProcessVerificationPaymentResult() {
				Result = ProcessVerificationPaymentResult.ResultEnum.Pending,
				RefundPaymentId = null,
			};

			return await mutexBuilder.LockAsync<ProcessVerificationPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CardPayment
						where p.Id == paymentId && p.Type == CardPaymentType.Verification && p.Status == CardPaymentStatus.Pending
						select p
					)
					.Include(p => p.Card)
					.Include(p => p.User)
					.ThenInclude(u => u.UserVerification)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					// not found
					if (payment == null) {
						ret.Result = ProcessVerificationPaymentResult.ResultEnum.NothingToDo;
						return ret;
					}

					try {
						await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, $"Charging {payment.AmountCents} cents");
					}
					catch { }

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();
					dbContext.Detach(payment);

					// charge
					ChargeResult result = null;
					try {
						result = await cardAcquirer.DoPaymentCharge(payment.GWTransactionId);
					}
					catch (Exception e) {
						logger?.Error(e, $"Failed to charge of payment #{payment.Id}");
					}

					// update ticket
					try {
						if (result?.Success ?? false) {
							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, "Charged successfully");
						}
						else {
							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Failed, "Charge failed");
						}
					}
					catch { }

					// assume failed by default
					payment.Status = CardPaymentStatus.Failed;
					payment.ProviderStatus = result?.ProviderStatus;
					payment.ProviderMessage = result?.ProviderMessage;
					payment.TimeCompleted = DateTime.UtcNow;
					// payment.TimeNextCheck = doesn't matter

					// payment will be updated
					dbContext.Update(payment);

					CardPayment refundEnqueued = null;

					// success
					if (result?.Success ?? false) {

						// new status
						payment.Status = CardPaymentStatus.Success;

						// new step on card verification
						payment.Card.State = CardState.Verification;
						dbContext.Update(payment.Card);

						// refund
						try {
							var refund = CreateRefundPayment(payment, payment.DeskTicketId);
							dbContext.CardPayment.Add(refund);
							refundEnqueued = refund;

							// charged and refunded
							ret.Result = ProcessVerificationPaymentResult.ResultEnum.Refunded;
							ret.RefundPaymentId = refund.Id;
						}
						catch (Exception e) {
							logger?.Error(e, $"Failed to enqueue verification refund for payment #{payment.Id}`");

							// refund failed
							ret.Result = ProcessVerificationPaymentResult.ResultEnum.RefundFailed;
						}
					}
					// failed
					else {
						payment.Card.State = CardState.Deleted;
						dbContext.Update(payment.Card);

						// didnt charge
						ret.Result = ProcessVerificationPaymentResult.ResultEnum.ChargeFailed;
					}

					try {
						await dbContext.SaveChangesAsync();
					} catch (Exception e) {
						logger?.Error(e);
						throw e;
					}

					// dont track
					dbContext.Detach(payment.Card, payment);

					// update ticket
					try {
						if (refundEnqueued != null) {

							// saved, dont track
							dbContext.Detach(refundEnqueued);

							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, $"Refund #{refundEnqueued.Id} enqueued");
						}
					}
					catch { }
				}
				return ret;
			});
		}

		/// <summary>
		/// Process pending refunds
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static async Task<bool> ProcessRefundPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(CardPaymentQueue));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			return await mutexBuilder.LockAsync(async (ok) => {
				if (ok) {
					// get payment from db
					var payment = await (
						from p in dbContext.CardPayment
						where p.Id == paymentId && p.Type == CardPaymentType.Refund && p.Status == CardPaymentStatus.Pending
						select p
					)
					.Include(p => p.User)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					if (payment == null || payment.RefPaymentId == null) return false;

					// get ref payment
					var refPayment = await (
						from p in dbContext.CardPayment
						where
						p.Id == payment.RefPaymentId.Value &&
						(p.Type == CardPaymentType.Deposit || p.Type == CardPaymentType.Verification) &&
						p.Status == CardPaymentStatus.Success
						select p
					)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					if (refPayment == null) return false;

					await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, $"Refunding {payment.AmountCents} cents");

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();
					dbContext.Detach(payment);

					// charge
					string resultGWTID = null;
					try {
						resultGWTID = await cardAcquirer.RefundPayment(new RefundPayment() {
							AmountCents = (int)payment.AmountCents,
							TransactionId = payment.TransactionId,
							RefGWTransactionId = refPayment.GWTransactionId,
						});
					}
					catch (Exception e) {
						logger?.Error(e, $"Failed to make charge of payment #{payment.Id} (refund of payment #{refPayment.Id})");
					}

					// update ticket
					try {
						if (resultGWTID != null) {
							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, "Refunded successfully");
						}
						else {
							await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Failed, "Refund failed");
						}
					}
					catch { }

					payment.Status = CardPaymentStatus.Failed;
					payment.TimeCompleted = DateTime.UtcNow;
					// payment.TimeNextCheck = doesn't matter

					if (resultGWTID != null) {

						// update payment
						payment.GWTransactionId = resultGWTID;
						payment.Status = CardPaymentStatus.Success;
						dbContext.Update(payment);
					}

					await dbContext.SaveChangesAsync();
					dbContext.Detach(payment);

					return payment.Status == CardPaymentStatus.Success;
				}
				return false;
			});
		}

		// ---

		/// <summary>
		/// New card payment tx ID
		/// </summary>
		public static string GenerateTransactionId() {
			return Guid.NewGuid().ToString("N");
		}

		/// <summary>
		/// Data input payment result
		/// </summary>
		public class ProcessPendingCardDataInputPaymentResult {

			public ResultEnum Result { get; set; }
			public long? VerificationPaymentId { get; set; }

			public enum ResultEnum {
				NothingToDo,
				Pending,
				DepositSuccess,
				WithdrawCardDataMismatched,
				FailedToChargeVerification,
				WithdrawSuccess
			}
		}

		/// <summary>
		/// Verification payment result
		/// </summary>
		public class ProcessVerificationPaymentResult {

			public ResultEnum Result { get; set; }
			public long? RefundPaymentId { get; set; }

			public enum ResultEnum {
				NothingToDo,
				Pending,
				ChargeFailed,
				RefundFailed,
				Refunded
			}
		}
	}
}
