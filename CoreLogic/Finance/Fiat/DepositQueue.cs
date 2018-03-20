using Goldmint.Common;
using Goldmint.CoreLogic.Services.Acquiring;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance.Fiat {

	public static class DepositQueue {

		/// <summary>
		/// Attempt to start deposit
		/// </summary>
		public static async Task<DepositResult> StartDepositWithCard(IServiceProvider services, long userId, UserTier userTier, CardPayment payment, long financialHistoryId) {

			if (payment.Type != CardPaymentType.Deposit) throw new ArgumentException("Incorrect payment type");
			if (payment.AmountCents <= 0) throw new ArgumentException("Amount must be greater than zero");
			if (payment.Card == null) throw new ArgumentException("Card not included");

			var logger = services.GetLoggerFor(typeof(DepositQueue));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var cardAcquirer = services.GetRequiredService<ICardAcquirer>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			return await StartDeposit(services, userId, userTier, payment.Currency, payment.AmountCents, async () => {

				try {
					await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, $"Charging {payment.AmountCents} cents, card payment #{payment.Id}");
				}
				catch { }

				// charge payment
				var chargeResult = await cardAcquirer.DoPaymentCharge(payment.GWTransactionId);
				try {
					payment.Status = chargeResult.Success ? CardPaymentStatus.Success : CardPaymentStatus.Failed;
					payment.ProviderMessage = chargeResult.ProviderMessage;
					payment.ProviderStatus = chargeResult.ProviderStatus;
					payment.TimeCompleted = DateTime.UtcNow;

					dbContext.Update(payment);
					await dbContext.SaveChangesAsync();
				}
				catch (Exception e) {
					logger?.Error(e, $"Failed to update payment status while charging deposit payment #{payment.Id}");
				}

				if (chargeResult.Success) {
					try {
						await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Pending, "Charged succesfully");
					}
					catch { }

					return new Deposit() {
						UserId = userId,
						Status = DepositStatus.Initial,
						Currency = payment.Currency,
						AmountCents = payment.AmountCents,
						RefFinancialHistoryId = financialHistoryId,
						Source = DepositSource.CreditCard,
						SourceId = payment.Id,
						DeskTicketId = payment.DeskTicketId,
						TimeCreated = DateTime.UtcNow,
						TimeNextCheck = DateTime.UtcNow,
					};
				}

				try {
					await ticketDesk.UpdateTicket(payment.DeskTicketId, UserOpLogStatus.Failed, "Charge failed");
				}
				catch { }
				return null;
			});
		}

		/// <summary>
		/// Attempt to start deposit
		/// </summary>
		public static async Task<DepositResult> StartDepositWithSwift(IServiceProvider services, long userId, UserTier userTier, SwiftRequest request, long financialHistoryId) {

			if (request.Type != SwiftPaymentType.Deposit) throw new ArgumentException("Incorrect payment type");
			if (request.AmountCents <= 0) throw new ArgumentException("Amount must be greater than zero");

			var logger = services.GetLoggerFor(typeof(DepositQueue));
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			return await StartDeposit(services, userId, userTier, request.Currency, request.AmountCents, async () => {

				try {
					await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Deposit of {request.AmountCents} cents, SWIFT request #{request.Id}");
				}
				catch { }

				return new Deposit() {
					UserId = userId,
					Status = DepositStatus.Initial,
					Currency = request.Currency,
					AmountCents = request.AmountCents,
					RefFinancialHistoryId = financialHistoryId,
					Source = DepositSource.SwiftRequest,
					SourceId = request.Id,
					DeskTicketId = request.DeskTicketId,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow,
				};
			});
		}

		/// <summary>
		/// Rollback failed withdrawal request
		/// </summary>
		public static async Task<DepositResult> StartDepositFromFailedWithdraw(IServiceProvider services, long userId, Withdraw withdraw, long financialHistoryId) {

			if (withdraw.Destination != WithdrawDestination.CreditCard) throw new ArgumentException("Incorrect withdrawal destination");
			if (withdraw.AmountCents <= 0) throw new ArgumentException("Amount must be greater than zero");

			var logger = services.GetLoggerFor(typeof(DepositQueue));

			return await StartDeposit(
				services: services, 
				userId: userId, 
				userTier: UserTier.Tier0,
				currency: withdraw.Currency, 
				amountCents: withdraw.AmountCents,
				ignoreLimits: true,
				onSuccess: () => Task.FromResult(
					new Deposit() {
						UserId = userId,
						Status = DepositStatus.Initial,
						Currency = withdraw.Currency,
						AmountCents = withdraw.AmountCents,
						RefFinancialHistoryId = financialHistoryId,
						Source = DepositSource.FailedWithdraw,
						SourceId = withdraw.Id,
						DeskTicketId = withdraw.DeskTicketId,
						TimeCreated = DateTime.UtcNow,
						TimeNextCheck = DateTime.UtcNow,
					}
				)
			);
		}

		/// <summary>
		/// Attempt to enqueue deposit
		/// </summary>
		public static async Task<DepositResult> StartDeposit(IServiceProvider services, long userId, UserTier userTier, FiatCurrency currency, long amountCents, Func<Task<Deposit>> onSuccess, bool ignoreLimits = false) {

			if (amountCents <= 0) throw new ArgumentException("Amount must be greater than zero");

			var logger = services.GetLoggerFor(typeof(DepositQueue));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			// lock deposit attempt for user
			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.DepositEnqueue, userId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					// check limits
					if (!ignoreLimits) {
						var limit = await User.GetCurrentFiatDepositLimit(services, currency, userId, userTier);
						if (amountCents > limit.Minimal) {
							return new DepositResult() {
								Status = FiatEnqueueResult.Limit,
							};
						}
					}

					try {
						var deposit = await onSuccess();
						if (deposit == null) throw new Exception("Got null deposit object");

						try {
							dbContext.Deposit.Add(deposit);
							await dbContext.SaveChangesAsync();
						}
						catch (Exception e) {
							await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Failed, "DB failed while deposit enqueue");
							throw e;
						}

						try {
							await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Pending, $"Deposit #{deposit.Id} successfully enqueued");
						}
						catch { }

						return new DepositResult() {
							Status = FiatEnqueueResult.Success,
							DepositId = deposit.Id,
						};
					}
					catch (Exception e) {
						return new DepositResult() {
							Status = FiatEnqueueResult.Error,
							Error = e,
						};
					}
				}

				return new DepositResult() {
					Status = FiatEnqueueResult.Error,
					Error = new Exception("Faield to lock deposit queue"),
				};
			});
		}

		/// <summary>
		/// Processes deposit record depending on it's current status
		/// </summary>
		public static async Task ProcessDeposit(IServiceProvider services, Deposit deposit) {

			if (deposit.User == null) throw new ArgumentException("User not included");
			if (deposit.RefFinancialHistory == null) throw new ArgumentException("Financial history not included");

			var logger = services.GetLoggerFor(typeof(DepositQueue));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.DepositCheck, deposit.Id)
			;

			await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					// oups, deposit is finalized already
					if (deposit.Status == DepositStatus.Success || deposit.Status == DepositStatus.Failed) {
						return;
					}

					try {
						// set next check time
						deposit.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(deposit.TimeCreated, TimeSpan.FromSeconds(15), 3);

						// initiate blockchain transaction
						if (deposit.Status == DepositStatus.Initial) {

							// update status to prevent double spending
							deposit.Status = DepositStatus.BlockchainInit;
							dbContext.Update(deposit);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							// launch transaction
							var ethTransactionId = await ethereumWriter.ChangeUserFiatBalance(
								userId: deposit.User.UserName,
								currency: deposit.Currency,
								amountCents: deposit.AmountCents
							);

							try {
								await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {ethTransactionId}");
							}
							catch { }

							// set new status
							deposit.EthTransactionId = ethTransactionId;
							deposit.RefFinancialHistory.RelEthTransactionId = deposit.EthTransactionId;
							deposit.Status = DepositStatus.BlockchainConfirm;
							dbContext.Update(deposit);
							dbContext.Update(deposit.RefFinancialHistory);
							await dbContext.SaveChangesAsync();

							// update ticket safely
							try {
								await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }
						}

						// initiating blockchain transaction
						else if (deposit.Status == DepositStatus.BlockchainInit) {
							// actually should not get into this section.
							// see initial status action above
						}

						// check confirmation
						else if (deposit.Status == DepositStatus.BlockchainConfirm) {
							var result = await ethereumReader.CheckTransaction(deposit.EthTransactionId);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								bool success = result == EthTransactionStatus.Success;

								deposit.Status = success ? DepositStatus.Success : DepositStatus.Failed;
								deposit.TimeCompleted = DateTime.UtcNow;

								deposit.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								deposit.RefFinancialHistory.TimeCompleted = deposit.TimeCompleted;

								dbContext.Update(deposit.RefFinancialHistory);
							}

							dbContext.Update(deposit);
							await dbContext.SaveChangesAsync();

							try {
								if (deposit.Status == DepositStatus.Success) {
									await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Completed, "Deposit has been saved on blockchain");
								}
								if (deposit.Status == DepositStatus.Failed) {
									await ticketDesk.UpdateTicket(deposit.DeskTicketId, UserOpLogStatus.Failed, "Deposit has NOT been saved on blockchain");
								}
							}
							catch { }
						}
					}
					catch (Exception e) {
						logger.Error(e, "Failed to update deposit #{0}", deposit.Id);
					}
				}
			});
		}

		// ---

		/// <summary>
		/// Deposit attempt result
		/// </summary>
		public sealed class DepositResult {

			public FiatEnqueueResult Status { get; internal set; }
			public long? DepositId { get; internal set; }
			public Exception Error { get; internal set; }
		}
	}
}
