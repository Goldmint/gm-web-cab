using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.The1StPayments;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Finance {

	public static class The1StPaymentsProcessing {

		/// <summary>
		/// New card input data operation to enqueue
		/// </summary>
		public static CreditCardPayment CreateCardDataInputPayment(UserCreditCard card, CardPaymentType type, string transactionId, string gwTransactionId, string oplogId, long amountCents) {

			// new deposit payment
			return new CreditCardPayment() {
				CardId = card.Id,
				TransactionId = transactionId,
				GwTransactionId = gwTransactionId,
				Type = type,
				UserId = card.UserId,
				Currency = FiatCurrency.Usd,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Unconfirmed,
				OplogId = oplogId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddMinutes(15),
			};
		}

		/// <summary>
		/// New verification payment
		/// </summary>
		private static async Task<CreditCardPayment> CreateVerificationPayment(IServiceProvider services, UserCreditCard card, string oplogId) {

			// if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<The1StPayments>();

			var amountCents = card.VerificationAmountCents;
			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartPaymentCharge3D(new StartPaymentCharge3D() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GwInitialDepositCardTransactionId,
				Purpose = "Card verification at goldmint.io"
			});

			return new CreditCardPayment() {
				CardId = card.Id,
				TransactionId = tid,
				GwTransactionId = gwTransactionId,
				Type = CardPaymentType.Verification,
				UserId = card.UserId,
				Currency = FiatCurrency.Usd,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Pending,
				OplogId = oplogId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		/// <summary>
		/// New deposit payment
		/// </summary>
		public static async Task<CreditCardPayment> CreateDepositPayment(IServiceProvider services, UserCreditCard card, FiatCurrency currency, long amountCents, long buyRequestId, string oplogId) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (card.State != CardState.Verified) throw new ArgumentException("Card not verified");
			// if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<The1StPayments>();

			// ---

			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartPaymentCharge3D(new StartPaymentCharge3D() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GwInitialDepositCardTransactionId,
				Purpose = "Deposit at goldmint.io",
				DynamicDescriptor = null,
			});

			return new CreditCardPayment() {
				CardId = card.Id,
				TransactionId = tid,
				GwTransactionId = gwTransactionId,
				Type = CardPaymentType.Deposit,
				UserId = card.UserId,
				Currency = currency,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Unconfirmed,
				RelatedExchangeRequestId = buyRequestId,
				OplogId = oplogId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		/// <summary>
		/// New payment refund
		/// </summary>
		private static CreditCardPayment CreateRefundPayment(CreditCardPayment refPayment, string oplogId) {

			if (!(
				refPayment.Type == CardPaymentType.CardDataInputSMS ||
			    refPayment.Type == CardPaymentType.Deposit ||
			    refPayment.Type == CardPaymentType.Verification
			)) {
				throw new ArgumentException("Ref payment has invalid type to refund");
			}
			if (refPayment.Status != CardPaymentStatus.Success) throw new ArgumentException("Cant refund unsuccessful payment");
			if (refPayment.CreditCard == null) throw new ArgumentException("Card not included");
			if (refPayment.AmountCents <= 0) throw new ArgumentException("Amount is invalid");
			// if (refPayment.User == null) throw new ArgumentException("User not included");

			// new refund payment
			return new CreditCardPayment() {
				CreditCard = refPayment.CreditCard,
				TransactionId = GenerateTransactionId(),
				GwTransactionId = "", // empty until charge
				RefPayment = refPayment,
				Type = CardPaymentType.Refund,
				UserId = refPayment.UserId,
				Currency = refPayment.Currency,
				AmountCents = refPayment.AmountCents,
				Status = CardPaymentStatus.Pending,
				OplogId = oplogId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(15 * 60),
			};
		}

		/// <summary>
		/// New withdraw payment
		/// </summary>
		public static async Task<CreditCardPayment> CreateWithdrawPayment(IServiceProvider services, UserCreditCard card, FiatCurrency currency, long amountCents, long sellRequestId, string oplogId) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (card.State != CardState.Verified) throw new ArgumentException("Card not verified");
			// if (card.User == null) throw new ArgumentException("User not included");

			var cardAcquirer = services.GetRequiredService<The1StPayments>();

			// ---

			var tid = GenerateTransactionId();

			var gwTransactionId = await cardAcquirer.StartCreditCharge(new StartCreditCharge() {
				AmountCents = (int)amountCents,
				TransactionId = tid,
				InitialGWTransactionId = card.GwInitialWithdrawCardTransactionId,
				Purpose = "Withdraw at goldmint.io",
				DynamicDescriptor = null,
			});

			return new CreditCardPayment() {
				CardId = card.Id,
				TransactionId = tid,
				GwTransactionId = gwTransactionId,
				Type = CardPaymentType.Withdraw,
				UserId = card.UserId,
				Currency = currency,
				AmountCents = amountCents,
				Status = CardPaymentStatus.Unconfirmed,
				RelatedExchangeRequestId = sellRequestId,
				OplogId = oplogId,
				TimeCreated = DateTime.UtcNow,
				TimeNextCheck = DateTime.UtcNow.AddSeconds(0),
			};
		}

		// ---

		/// <summary>
		/// Process card data input transaction (api call)
		/// </summary>
		public static async Task<ProcessPendingCardDataInputPaymentResult> ProcessPendingCardDataInputPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(The1StPaymentsProcessing));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<The1StPayments>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();
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

			return await mutexBuilder.CriticalSection<ProcessPendingCardDataInputPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CreditCardPayment
						where
						p.Id == paymentId &&
						(
							p.Type == CardPaymentType.CardDataInputSMS ||
							p.Type == CardPaymentType.CardDataInputCRD || 
							p.Type == CardPaymentType.CardDataInputP2P
						) &&
						p.Status == CardPaymentStatus.Pending
						select p
					)
						.Include(p => p.CreditCard)
						.Include(p => p.User).ThenInclude(u => u.UserVerification)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;

					// not found
					if (payment == null) {
						ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.NotFound;
						return ret;
					}

					// update payment
					bool finalized = false;
					string cardHolder = null;
					string cardMask = null;
					{

						// query acquirer
						var result = await cardAcquirer.CheckCardStored(payment.GwTransactionId);

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

						// set additional fields
						payment.ProviderStatus = result.ProviderStatus;
						payment.ProviderMessage = result.ProviderMessage;
						payment.TimeNextCheck = DateTime.UtcNow.AddMinutes(15);

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

					// now is final
					if (finalized) {

						CreditCardPayment verificationPaymentEnqueued = null;
						var card = payment.CreditCard;

						// delete card on any data mismatch
						var cardPrevState = card.State;
						card.State = CardState.Deleted;

						if (payment.Status == CardPaymentStatus.Success) {

							// refund payment if amount is non-zero
							if (payment.AmountCents > 0) {
								try {
									var refund = CreateRefundPayment(payment, payment.OplogId);
									dbContext.CreditCardPayment.Add(refund);
									try {
										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Refund for card store step is enqueued for payment #{payment.Id}");
									} catch { }
								}
								catch (Exception e) {
									logger?.Error(e, $"[1STP] Failed to enqueue card input refund for payment #{payment.Id}");
									try {
										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, $"Refund for card store step is failed to enqueue  for payment #{payment.Id}");
									}
									catch { }
								}
							}

							// set next step
							if (cardHolder != null && cardMask != null) {

								// check for duplicate
								if (
									await dbContext.UserCreditCard.CountAsync(_ =>
										_.UserId == payment.UserId &&
										_.State != CardState.Deleted &&
										_.Id != card.Id &&
										_.CardMask == cardMask
									) > 0
								) {

									card.State = CardState.Deleted;
									card.HolderName = cardHolder;
									card.CardMask = cardMask;

									try {
										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, $"Card with the same mask exists {cardMask}");
									}
									catch { }

									ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.DuplicateCard;
								}

								// [!] ONE STEP FLOW
								// this is 1st step - deposit data
								else if (cardPrevState == CardState.InputDepositData) {

									card.HolderName = cardHolder;
									card.CardMask = cardMask;

									try {
										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Provided card data on first step is saved");
									}
									catch { }

									card.State = CardState.Payment;

									// enqueue verification payment
									try {
										var verPayment = await CreateVerificationPayment(
											services: services,
											card: card,
											oplogId: payment.OplogId
										);
										dbContext.CreditCardPayment.Add(verPayment);
										verificationPaymentEnqueued = verPayment;

										// ok, for now
										ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.WithdrawDataOk;
									}
									catch (Exception e) {
										logger?.Error(e, $"[1STP] Failed to start verification charge for this payment");

										// failed to charge
										ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.FailedToChargeVerification;
									}
								}

								/*
								[!] TWO STEPS FLOW
								// this is 1st step - deposit data
								else if (cardPrevState == CardState.InputDepositData) {

									card.State = CardState.InputWithdrawData;
									card.HolderName = cardHolder;
									card.CardMask = cardMask;

									try {
										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Provided card data on first step is saved");
									}
									catch { }

									// ok
									ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.DepositDataOk;
								}

								 // [!] TWO STEPS FLOW
								 //this is 2nd step - withdraw data - must be the same card
								else if (cardPrevState == CardState.InputWithdrawData && card.CardMask != null) {

									var allowAnyCard = hostingEnv.IsDevelopment() || hostingEnv.IsStaging();

									// mask matched
									if (card.CardMask == cardMask || allowAnyCard) {

										card.State = CardState.Payment;

										try {
											await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Provided card data on second step is saved");
										}
										catch { }

										// enqueue verification payment
										try {
											var verPayment = await CreateVerificationPayment(
												services: services,
												card: card,
												oplogId: payment.OplogId
											);
											dbContext.CreditCardPayment.Add(verPayment);
											verificationPaymentEnqueued = verPayment;

											// ok, for now
											ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.WithdrawDataOk;
										}
										catch (Exception e) {
											logger?.Error(e, $"[1STP] Failed to start verification charge for this payment");

											// failed to charge
											ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.FailedToChargeVerification;
										}
									}
									else {
										// mask mismatched
										ret.Result = ProcessPendingCardDataInputPaymentResult.ResultEnum.WithdrawCardDataMismatched;

										await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, "Provided card data is mismatched");
									}
								}
								*/
							}
							else {
								try { 
									await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, $"Did not get card holder or card mask from gateway");
								} catch { }
							}
						}
						else if (payment.Status == CardPaymentStatus.Failed) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, $"Card data input step is unsuccessful on a gateway side");
						}

						// update card state
						dbContext.Update(card);
						await dbContext.SaveChangesAsync();

						if (verificationPaymentEnqueued != null) {
							ret.VerificationPaymentId = verificationPaymentEnqueued.Id;

							try {
								await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Verification payment #{verificationPaymentEnqueued.Id} enqueued");
							}
							catch { }
						}
					}
				}

				return ret;
			});
		}

		/// <summary>
		/// Charge verification payment and enqueue refund (core-worker or api call)
		/// </summary>
		public static async Task<ProcessVerificationPaymentResult> ProcessVerificationPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(The1StPaymentsProcessing));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<The1StPayments>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

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

			return await mutexBuilder.CriticalSection<ProcessVerificationPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CreditCardPayment
						where p.Id == paymentId && p.Type == CardPaymentType.Verification && p.Status == CardPaymentStatus.Pending
						select p
					)
						.Include(p => p.CreditCard)
						.Include(p => p.User)
						.ThenInclude(u => u.UserVerification)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;

					// not found
					if (payment == null) {
						ret.Result = ProcessVerificationPaymentResult.ResultEnum.NotFound;
						return ret;
					}

					try {
						await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Charging {payment.AmountCents} cents");
					}
					catch { }

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();

					// charge
					ChargeResult result = null;
					try {
						result = await cardAcquirer.DoPaymentCharge3D(payment.GwTransactionId);
					}
					catch (Exception e) {
						logger?.Error(e, $"[1STP] Failed to process payment #{payment.Id} (verification)");
					}

					// update ticket
					try {
						if (result?.Success ?? false) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Charged successfully");
						}
						else {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, "Charge failed");
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

					CreditCardPayment refundEnqueued = null;

					// success
					if (result?.Success ?? false) {

						// new status
						payment.Status = CardPaymentStatus.Success;

						// new step on card verification
						payment.CreditCard.State = CardState.Verification;
						dbContext.Update(payment.CreditCard);

						// refund
						try {
							var refund = CreateRefundPayment(payment, payment.OplogId);
							dbContext.CreditCardPayment.Add(refund);
							refundEnqueued = refund;

							// charged and refunded
							ret.Result = ProcessVerificationPaymentResult.ResultEnum.Refunded;
							ret.RefundPaymentId = refund.Id;
						}
						catch (Exception e) {
							logger?.Error(e, $"[1STP] Failed to enqueue verification refund for payment #{payment.Id}`");

							// refund failed
							ret.Result = ProcessVerificationPaymentResult.ResultEnum.RefundFailed;
						}
					}
					// failed
					else {
						payment.CreditCard.State = CardState.Deleted;
						dbContext.Update(payment.CreditCard);

						// didnt charge
						ret.Result = ProcessVerificationPaymentResult.ResultEnum.ChargeFailed;
					}

					// save
					await dbContext.SaveChangesAsync();

					// update ticket
					try {
						if (refundEnqueued != null) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Refund #{refundEnqueued.Id} enqueued");
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
		public static async Task<bool> ProcessRefundPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(The1StPaymentsProcessing));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<The1StPayments>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {
					// get payment from db
					var payment = await (
						from p in dbContext.CreditCardPayment
						where 
							p.Id == paymentId && 
							p.Type == CardPaymentType.Refund && 
							p.Status == CardPaymentStatus.Pending &&
							p.AmountCents > 0
						select p
					)
					.Include(p => p.User)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					if (payment?.RelPaymentId == null) return false;

					// get ref payment
					var refPayment = await (
						from p in dbContext.CreditCardPayment
						where
						p.Id == payment.RelPaymentId.Value &&
						(p.Type == CardPaymentType.CardDataInputSMS || p.Type == CardPaymentType.Deposit || p.Type == CardPaymentType.Verification) &&
						p.Status == CardPaymentStatus.Success
						select p
					)
					.AsNoTracking()
					.FirstOrDefaultAsync();

					if (refPayment == null) return false;

					await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Refunding {payment.AmountCents} cents");

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();

					// charge
					string resultGwTxId = null;
					try {
						resultGwTxId = await cardAcquirer.RefundPayment(new RefundPayment() {
							AmountCents = (int)payment.AmountCents,
							TransactionId = payment.TransactionId,
							RefGWTransactionId = refPayment.GwTransactionId,
						});
					}
					catch (Exception e) {
						logger?.Error(e, $"[1STP] Failed to charge payment #{payment.Id} (refund of payment #{refPayment.Id})");
					}

					// update ticket
					try {
						if (resultGwTxId != null) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Completed, "Refunded successfully");
						}
						else {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, $"Card verification refund #{payment.Id} failed and requires manual processing");
						}
					}
					catch { }

					payment.Status = CardPaymentStatus.Failed;
					payment.ProviderStatus = "Refund Failed";
					payment.ProviderMessage = "Refund Failed";
					payment.TimeCompleted = DateTime.UtcNow;
					// payment.TimeNextCheck = doesn't matter

					// update payment
					if (resultGwTxId != null) {
						payment.Status = CardPaymentStatus.Success;
						payment.GwTransactionId = resultGwTxId;
						payment.ProviderStatus = "Refund Success";
						payment.ProviderMessage = "Refund Success";
					}

					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();

					return payment.Status == CardPaymentStatus.Success;
				}
				return false;
			});
		}

		/// <summary>
		/// Process deposit payment
		/// </summary>
		public static async Task<ProcessDepositPaymentResult> ProcessDepositPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(The1StPaymentsProcessing));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<The1StPayments>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			// by default
			var ret = new ProcessDepositPaymentResult() {
				Result = ProcessDepositPaymentResult.ResultEnum.Pending,
			};

			return await mutexBuilder.CriticalSection<ProcessDepositPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CreditCardPayment
						where 
							p.Id == paymentId && 
							p.Type == CardPaymentType.Deposit && 
							p.Status == CardPaymentStatus.Pending
						select p
					)
						.Include(p => p.User)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;

					// not found
					if (payment == null) {
						ret.Result = ProcessDepositPaymentResult.ResultEnum.NotFound;
						return ret;
					}

					try {
						await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Charging { TextFormatter.FormatAmount(payment.AmountCents, payment.Currency) }");
					}
					catch { }

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();

					// charge
					ChargeResult result = null;
					try {
						result = await cardAcquirer.DoPaymentCharge3D(payment.GwTransactionId);
					}
					catch (Exception e) {
						logger?.Error(e, $"[1STP] Failed to process payment #{payment.Id} (deposit)");
					}

					// update ticket
					try {
						if (result?.Success ?? false) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Charged successfully");
						}
						else {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, "Charge failed");
						}
					}
					catch { }

					// assume failed by default
					payment.Status = CardPaymentStatus.Failed;
					payment.ProviderStatus = result?.ProviderStatus;
					payment.ProviderMessage = result?.ProviderMessage;
					payment.TimeCompleted = DateTime.UtcNow;
					// payment.TimeNextCheck = doesn't matter
					ret.Result = ProcessDepositPaymentResult.ResultEnum.Failed;

					// success
					if (result?.Success ?? false) {
						payment.Status = CardPaymentStatus.Success;
						ret.Result = ProcessDepositPaymentResult.ResultEnum.Charged;
					}

					// payment will be updated
					dbContext.Update(payment);

					// save
					await dbContext.SaveChangesAsync();
				}
				return ret;
			});
		}

		/// <summary>
		/// Process deposit payment
		/// </summary>
		public static async Task<ProcessWithdrawalPaymentResult> ProcessWithdrawPayment(IServiceProvider services, long paymentId) {

			var logger = services.GetLoggerFor(typeof(The1StPaymentsProcessing));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<The1StPayments>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

			// lock payment updating by payment id
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CardPaymentCheck, paymentId)
			;

			// by default
			var ret = new ProcessWithdrawalPaymentResult() {
				Result = ProcessWithdrawalPaymentResult.ResultEnum.Pending,
			};

			return await mutexBuilder.CriticalSection<ProcessWithdrawalPaymentResult>(async (ok) => {
				if (ok) {

					// get payment from db
					var payment = await (
						from p in dbContext.CreditCardPayment
						where 
							p.Id == paymentId && 
							p.Type == CardPaymentType.Withdraw && 
							p.Status == CardPaymentStatus.Pending
						select p
					)
						.Include(p => p.User)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;

					// not found
					if (payment == null) {
						ret.Result = ProcessWithdrawalPaymentResult.ResultEnum.NotFound;
						return ret;
					}

					try {
						await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, $"Withdraw of { TextFormatter.FormatAmount(payment.AmountCents, payment.Currency) }");
					}
					catch { }

					// prevent double spending
					payment.Status = CardPaymentStatus.Charging;
					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();

					// charge
					ChargeResult result = null;
					try {
						result = await cardAcquirer.DoCreditCharge(payment.GwTransactionId);
					}
					catch (Exception e) {
						logger?.Error(e, $"[1STP] Failed to process payment #{payment.Id} (withdraw)");
					}

					// update ticket
					try {
						if (result?.Success ?? false) {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Pending, "Withdrawn successfully");
						}
						else {
							await ticketDesk.Update(payment.OplogId, UserOpLogStatus.Failed, "Withdrawal payment failed");
						}
					}
					catch { }

					// assume failed by default
					payment.Status = CardPaymentStatus.Failed;
					payment.ProviderStatus = result?.ProviderStatus;
					payment.ProviderMessage = result?.ProviderMessage;
					payment.TimeCompleted = DateTime.UtcNow;
					// payment.TimeNextCheck = doesn't matter
					ret.Result = ProcessWithdrawalPaymentResult.ResultEnum.Failed;

					// payment will be updated
					dbContext.Update(payment);

					// success
					if (result?.Success ?? false) {
						payment.Status = CardPaymentStatus.Success;
						ret.Result = ProcessWithdrawalPaymentResult.ResultEnum.Withdrawn;
					}

					// save
					await dbContext.SaveChangesAsync();
				}
				return ret;
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
				NotFound,
				Pending,
				DuplicateCard,
				DepositDataOk,
				FailedToCharge3DTransaction,
				WithdrawCardDataMismatched,
				FailedToChargeVerification,
				WithdrawDataOk
			}
		}

		/// <summary>
		/// Verification payment result
		/// </summary>
		public class ProcessVerificationPaymentResult {

			public ResultEnum Result { get; set; }
			public long? RefundPaymentId { get; set; }

			public enum ResultEnum {
				NotFound,
				Pending,
				ChargeFailed,
				RefundFailed,
				Refunded
			}
		}

		/// <summary>
		/// Deposit payment result
		/// </summary>
		public class ProcessDepositPaymentResult {

			public ResultEnum Result { get; set; }

			public enum ResultEnum {
				NotFound,
				Pending,
				Failed,
				Charged,
			}
		}
		
		/// <summary>
		/// Withdrawal payment result
		/// </summary>
		public class ProcessWithdrawalPaymentResult {

			public ResultEnum Result { get; set; }

			public enum ResultEnum {
				NotFound,
				Pending,
				Failed,
				Withdrawn,
			}
		}
	}
}
