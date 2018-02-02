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
		public static async Task<WithdrawResult> StartWithdrawWithCard(IServiceProvider services, CardPayment payment, FinancialHistory financialHistory) {

			if (payment.Type != CardPaymentType.Withdraw) throw new ArgumentException("Incorrect payment type");
			if (payment.AmountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (payment.Card == null) throw new ArgumentException("Card not included");
			if (payment.User == null) throw new ArgumentException("User not included");

			var logger = services.GetLoggerFor(typeof(WithdrawQueue));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			return await StartWithdraw(services, payment.User, payment.Currency, payment.AmountCents, () => {
				return Task.FromResult(
					new Withdraw() {
						User = payment.User,
						Status = WithdrawStatus.Initial,
						Currency = payment.Currency,
						AmountCents = payment.AmountCents,
						RefFinancialHistoryId = financialHistory.Id,
						Destination = WithdrawDestination.CreditCard,
						DestinationId = payment.Id,
						DeskTicketId = payment.DeskTicketId,
						TimeCreated = DateTime.UtcNow,
						TimeNextCheck = DateTime.UtcNow,
					}
				);
			});
		}

		/// <summary>
		/// Attempt to enqueue withdraw
		/// </summary>
		public static async Task<WithdrawResult> StartWithdraw(IServiceProvider services, User user, FiatCurrency currency, long amountCents, Func<Task<Withdraw>> onSuccess) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");

			var logger = services.GetLoggerFor(typeof(WithdrawQueue));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			// lock withdraw attempt for user
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.WithdrawEnqueue, user.Id)
			;

			return await mutexBuilder.LockAsync(async (ok) => {
				if (ok) {

					// get limit
					var limit = await UserAccount.GetCurrentFiatWithdrawLimit(services, currency, user);
					if (amountCents <= limit) {
						try {
							var withdraw = await onSuccess();
							if (withdraw == null) throw new Exception("Got null withdraw object");

							try {
								dbContext.Withdraw.Add(withdraw);
								await dbContext.SaveChangesAsync();
								dbContext.Detach(withdraw);
							}
							catch (Exception e) {
								await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Cancelled, "DB failed while withdraw enqueue");
								throw e;
							}

							try {
								await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Opened, "Withdraw successfully enqueued");
							}
							catch { }

							return new WithdrawResult() {
								Status = FiatEnqueueStatus.Success,
								WithdrawId = withdraw.Id,
							};
						}
						catch (Exception e) {
							return new WithdrawResult() {
								Status = FiatEnqueueStatus.Error,
								Error = e,
							};
						}
					}
					return new WithdrawResult() {
						Status = FiatEnqueueStatus.Limit,
					};
				}
				else {
					return new WithdrawResult() {
						Status = FiatEnqueueStatus.Error,
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
			if (withdraw.FinancialHistory == null) throw new ArgumentException("Financial history not included");

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

			await mutexBuilder.LockAsync(async (ok) => {
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
							dbContext.Detach(withdraw);

							try {
								await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Opened, "Blockchain transaction initiated");
							}
							catch { }

							// launch transaction
							var txid = await ethereumWriter.ChangeUserFiatBalance(withdraw.User.Id, withdraw.Currency, -1 * withdraw.AmountCents);
							withdraw.EthTransactionId = txid;

							// set new status
							withdraw.Status = WithdrawStatus.BlockchainConfirm;

							// save
							dbContext.Update(withdraw);
							await dbContext.SaveChangesAsync();
							dbContext.Detach(withdraw);

							try {
								await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Opened, "Blockchain transaction checking started");
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
							if (result == BlockchainTransactionStatus.Success || result == BlockchainTransactionStatus.Failed) {

								var success = result == BlockchainTransactionStatus.Success;

								withdraw.Status = success ? WithdrawStatus.Success : WithdrawStatus.Failed;
								withdraw.TimeCompleted = DateTime.UtcNow;

								withdraw.FinancialHistory.Status = success ? FinancialHistoryStatus.Success : FinancialHistoryStatus.Cancelled;
								withdraw.FinancialHistory.TimeCompleted = withdraw.TimeCompleted;
								dbContext.Update(withdraw.FinancialHistory);
							}

							// finalize
							if (withdraw.Status == WithdrawStatus.Success) {

								await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Opened, "Withdraw has been saved on blockchain");

								bool sendToSupport = true;

								// pay to card
								if (withdraw.Destination == WithdrawDestination.CreditCard) {
									try {
										await SendCardWithdraw(services, withdraw);
										sendToSupport = false;

										try {
											await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Success, "Withdraw completed with credit card payment");
										}
										catch { }
									} catch (Exception e) {
										logger?.Error(e, $"Failed to complete credit card withdraw #{withdraw.Id}");
									}
								}

								try {
									if (sendToSupport) {
										await ticketDesk.CreateSupportWithdrawTicket(withdraw.DeskTicketId, withdraw, "Withdraw required manual processing");
										await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Opened, "Request has been sent to support team");
									}
								}
								catch { }
							}
							if (withdraw.Status == WithdrawStatus.Failed) {
								try {
									await ticketDesk.UpdateCardWithdrawTicket(withdraw.DeskTicketId, TicketStatus.Cancelled, "Withdraw has not been saved on blockchain");
								}
								catch { }
							}

							dbContext.Update(withdraw);
							await dbContext.SaveChangesAsync();
							dbContext.Detach(withdraw, withdraw.FinancialHistory);
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
		private static async Task SendCardWithdraw(IServiceProvider services, Withdraw withdraw) {

			if (withdraw.Destination != WithdrawDestination.CreditCard) throw new ArgumentException("Illegal withdraw destination");

			var appConfig = services.GetRequiredService<AppConfig>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var logger = services.GetLoggerFor(typeof(WithdrawQueue));

			var payment = await(
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
			} catch (Exception e) {
				logger?.Error(e, $"Failed to charge of withdraw payment #{payment.Id}");
			}

			payment.Status = (chargeResult?.Success ?? false) ? CardPaymentStatus.Success: CardPaymentStatus.Failed;
			payment.ProviderMessage = chargeResult?.ProviderMessage;
			payment.ProviderStatus = chargeResult?.ProviderStatus;
			payment.TimeCompleted = DateTime.UtcNow;

			dbContext.Update(payment);
			await dbContext.SaveChangesAsync();
			dbContext.Detach(payment);
		}

		// ---

		/// <summary>
		/// Withdraw attempt result
		/// </summary>
		public sealed class WithdrawResult {

			public FiatEnqueueStatus Status { get; internal set; }
			public long? WithdrawId { get; internal set; }
			public Exception Error { get; internal set; }
		}
	}
}
