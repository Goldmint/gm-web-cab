using Goldmint.Common;
using Goldmint.CoreLogic.Services.Acquiring;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance.Fiat {

	public static class WithdrawQueue {

		/// <summary>
		/// Attempt to start deposit
		/// </summary>
		public static async Task<WithdrawResult> StartWithdrawWithCard(IServiceProvider services, long userId, UserTier userTier, CardPayment payment, long financialHistoryId) {

			if (payment.Type != CardPaymentType.Withdraw) throw new ArgumentException("Incorrect payment type");
			if (payment.AmountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (payment.Card == null) throw new ArgumentException("Card not included");

			// var logger = services.GetLoggerFor(typeof(WithdrawQueue));
			// var dbContext = services.GetRequiredService<ApplicationDbContext>();
			// var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			// var ticketDesk = services.GetRequiredService<ITicketDesk>();

			return await StartWithdraw(services, userId, userTier, payment.Currency, payment.AmountCents, () => Task.FromResult(
				new Withdraw() {
					UserId = payment.UserId,
					Status = WithdrawStatus.Initial,
					Currency = payment.Currency,
					AmountCents = payment.AmountCents,
					RefFinancialHistoryId = financialHistoryId,
					Destination = WithdrawDestination.CreditCard,
					DestinationId = payment.Id,
					DeskTicketId = payment.DeskTicketId,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow,
				}
			));
		}

		/// <summary>
		/// Attempt to enqueue withdraw
		/// </summary>
		public static async Task<WithdrawResult> StartWithdraw(IServiceProvider services, long userId, UserTier userTier, FiatCurrency currency, long amountCents, Func<Task<Withdraw>> onSuccess) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");

			var logger = services.GetLoggerFor(typeof(WithdrawQueue));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			// lock withdraw attempt for user
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.WithdrawEnqueue, userId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					// get limit
					var limit = await User.GetCurrentFiatWithdrawLimit(services, currency, userId, userTier);
					if (amountCents <= limit.Minimal) {
						try {
							var withdraw = await onSuccess();
							if (withdraw == null) throw new Exception("Got null withdraw object");

							try {
								dbContext.Withdraw.Add(withdraw);
								await dbContext.SaveChangesAsync();
							}
							catch (Exception e) {
								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Failed, "DB failed while withdraw enqueue");
								throw e;
							}

							try {
								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, $"Withdraw #{withdraw.Id} successfully enqueued");
							}
							catch { }

							return new WithdrawResult() {
								Status = FiatEnqueueResult.Success,
								WithdrawId = withdraw.Id,
							};
						}
						catch (Exception e) {
							return new WithdrawResult() {
								Status = FiatEnqueueResult.Error,
								Error = e,
							};
						}
					}
					return new WithdrawResult() {
						Status = FiatEnqueueResult.Limit,
					};
				}
				else {
					return new WithdrawResult() {
						Status = FiatEnqueueResult.Error,
						Error = new Exception("Faield to lock deposit queue"),
					};
				}
			});
		}

		/// <summary>
		/// Processes withdraw record depending on it's current status
		/// </summary>
		public static async Task ProcessWithdraw(IServiceProvider services, Withdraw withdraw) {

			if (withdraw.User == null) throw new ArgumentException("User not included");
			if (withdraw.RefFinancialHistory == null) throw new ArgumentException("Financial history not included");

			var logger = services.GetLoggerFor(typeof(WithdrawQueue));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.WithdrawCheck, withdraw.Id)
			;

			await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					// oups, finalized already
					if (withdraw.Status == WithdrawStatus.Success || withdraw.Status == WithdrawStatus.Failed) {
						return;
					}

					try {
						// set next check time
						withdraw.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(withdraw.TimeCreated, TimeSpan.FromSeconds(15), 3);

						// initiate blockchain transaction
						if (withdraw.Status == WithdrawStatus.Initial) {

							// update status to prevent double spending
							withdraw.Status = WithdrawStatus.BlockchainInit;
							dbContext.Update(withdraw);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							// launch transaction
							var ethTransactionId = await ethereumWriter.ChangeUserFiatBalance(
								userId: withdraw.User.UserName, 
								currency: withdraw.Currency,
								amountCents: -1 * withdraw.AmountCents
							);

							try {
								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {ethTransactionId}");
							}
							catch { }

							// set new status
							withdraw.EthTransactionId = ethTransactionId;
							withdraw.RefFinancialHistory.RelEthTransactionId = withdraw.EthTransactionId;
							withdraw.Status = WithdrawStatus.BlockchainConfirm;

							// save
							dbContext.Update(withdraw.RefFinancialHistory);
							dbContext.Update(withdraw);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }
						}

						// initiating blockchain transaction
						else if (withdraw.Status == WithdrawStatus.BlockchainInit) {
							// actually should not get into this section.
							// see initial status action above
						}

						// check confirmation
						else if (withdraw.Status == WithdrawStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(withdraw.EthTransactionId);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								var success = result == EthTransactionStatus.Success;

								withdraw.Status = success ? WithdrawStatus.Success : WithdrawStatus.Failed;
								withdraw.TimeCompleted = DateTime.UtcNow;

								withdraw.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Success : FinancialHistoryStatus.Cancelled;
								withdraw.RefFinancialHistory.TimeCompleted = withdraw.TimeCompleted;
								dbContext.Update(withdraw.RefFinancialHistory);
							}

							// finalize
							if (withdraw.Status == WithdrawStatus.Success) {

								await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, "Withdraw has been saved on blockchain");

								// pay to card
								if (withdraw.Destination == WithdrawDestination.CreditCard) {
									try {
										var wdrPayment = await SendCardWithdraw(services, withdraw);

#if DEBUG
										wdrPayment.Status = CardPaymentStatus.Failed;
#endif

										if (wdrPayment.Status == CardPaymentStatus.Success) {
											try {
												await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Completed, $"Withdraw completed with credit card payment #{wdrPayment.Id}");
											}
											catch { }
										}
										else {
											try {
												await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Pending, $"Withdrawal card payment #{wdrPayment.Id} failed. Reverting blockchain withdrawal");
											}
											catch { }

											// fin history
											var finHistory = new DAL.Models.FinancialHistory() {
												Type = FinancialHistoryType.Deposit,
												AmountCents = withdraw.AmountCents,
												FeeCents = 0,
												DeskTicketId = withdraw.DeskTicketId,
												Status = FinancialHistoryStatus.Pending,
												TimeCreated = DateTime.UtcNow,
												UserId = withdraw.UserId,
												Comment = $"Reverting failed withdrawal payment #{withdraw.DestinationId}",
											};
											dbContext.FinancialHistory.Add(finHistory);

											// save
											dbContext.Update(withdraw);
											await dbContext.SaveChangesAsync();

											// enqueue deposit
											var res = await DepositQueue.StartDepositFromFailedWithdraw(
												services: services,
												userId: withdraw.UserId,
												withdraw: withdraw,
												financialHistoryId: finHistory.Id
											);

											// failed
											if (res.Status != FiatEnqueueResult.Success) {
												dbContext.FinancialHistory.Remove(finHistory);
												await dbContext.SaveChangesAsync();

												try {
													await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Failed, $"Failed to enqueue deposit");
												}
												catch {
												}
											}
										}
									}
									catch (Exception e) {
										logger?.Error(e, $"Failed to complete credit card withdraw #{withdraw.Id}");
									}
								}
							}
							if (withdraw.Status == WithdrawStatus.Failed) {
								try {
									await ticketDesk.UpdateTicket(withdraw.DeskTicketId, UserOpLogStatus.Failed, "Withdraw has NOT been saved on blockchain");
								}
								catch { }
							}

							dbContext.Update(withdraw);
							await dbContext.SaveChangesAsync();
						}
					}
					catch (Exception e) {
						logger?.Error(e, "Failed to update withdraw #{0}", withdraw.Id);
					}
				}
			});
		}

		/// <summary>
		/// Completes previously initiated credit operation or falls with exception
		/// </summary>
		private static async Task<CardPayment> SendCardWithdraw(IServiceProvider services, Withdraw withdraw) {

			if (withdraw.Destination != WithdrawDestination.CreditCard) throw new ArgumentException("Illegal withdraw destination");

			var appConfig = services.GetRequiredService<AppConfig>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var logger = services.GetLoggerFor(typeof(WithdrawQueue));

			var payment = await (
				from p in dbContext.CardPayment
				where
				p.Id == withdraw.DestinationId &&
				p.Type == CardPaymentType.Withdraw &&
				p.UserId == withdraw.UserId &&
				p.Status == CardPaymentStatus.Pending
				select p
			)
				.Include(_ => _.Card)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (payment?.Card == null) throw new Exception("Payment or card not found");

			ChargeResult chargeResult = null;
			try {
				chargeResult = await cardAcquirer.DoCreditCharge(payment.GWTransactionId);
			}
			catch (Exception e) {
				logger?.Error(e, $"Failed to charge of withdraw payment #{payment.Id}");
			}

			payment.Status = (chargeResult?.Success ?? false) ? CardPaymentStatus.Success : CardPaymentStatus.Failed;
			payment.ProviderMessage = chargeResult?.ProviderMessage;
			payment.ProviderStatus = chargeResult?.ProviderStatus;
			payment.TimeCompleted = DateTime.UtcNow;

			dbContext.Update(payment);
			await dbContext.SaveChangesAsync();

			return payment;
		}

		// ---

		/// <summary>
		/// Withdraw attempt result
		/// </summary>
		public sealed class WithdrawResult {

			public FiatEnqueueResult Status { get; internal set; }
			public long? WithdrawId { get; internal set; }
			public Exception Error { get; internal set; }
		}
	}
}
