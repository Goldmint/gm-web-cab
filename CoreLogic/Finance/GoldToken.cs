using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance {

	public static class GoldToken {

		/// <summary>
		/// Process GOLD buying request (core-worker harvester)
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> ProcessContractBuyRequest(IServiceProvider services, long internalRequestId, string address, BigInteger amount, string transactionId) {

			if (internalRequestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellRequestProcessingResult.InvalidArgs;
			if (amount < 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(transactionId)) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.BuyGoldRequest
				where
					r.Input == BuyGoldRequestInput.ContractEthPayment &&
					r.Id == internalRequestId &&
					r.Status == BuyGoldRequestStatus.Confirmed &&
					r.Output == BuyGoldRequestOutput.EthereumAddress &&
					r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellRequestProcessingResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.GoldBuyingReq, internalRequestId)
			;

			return await mutexBuilder.CriticalSection<BuySellRequestProcessingResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of `{ TextFormatter.FormatTokenAmount(amount, Common.Tokens.ETH.Decimals) }` ETH is `{transactionId}`");
					}
					catch { }

					try {

						var timeNow = DateTime.UtcNow;

						// ok
						if (request.TimeExpires > timeNow) {

							var suppRequest = new BuyGoldCryptoSupportRequest() {

								Status = SupportRequestStatus.Pending,
								AmountWei = amount.ToString(),
								BuyGoldRequestId = request.Id,
								TimeCreated = timeNow,

								UserId = request.UserId,
								OplogId = request.OplogId,
								RefUserFinHistoryId = request.RefUserFinHistoryId,
							};
							dbContext.BuyGoldCryptoSupportRequest.Add(suppRequest);

							request.Status = BuyGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. New support request #{suppRequest.Id} enqueued");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Success;
						}

						// expired
						else {

							request.Status = BuyGoldRequestStatus.Expired;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Failed, $"Request #{request.Id} is expired");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Expired;
						}
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process buy request #{request.Id}");
					}
				}
				return BuySellRequestProcessingResult.MutexFailure;
			});
		}

		/// <summary>
		/// Process GOLD selling request (core-worker harvester)
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> ProcessContractSellRequest(IServiceProvider services, long internalRequestId, string address, BigInteger amount, string transactionId) {

			if (internalRequestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellRequestProcessingResult.InvalidArgs;
			if (amount < 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(transactionId)) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.SellGoldRequest
				where
					r.Input == SellGoldRequestInput.ContractGoldBurning &&
					r.Id == internalRequestId &&
					r.Status == SellGoldRequestStatus.Confirmed &&
					r.Output == SellGoldRequestOutput.Eth &&
					r.OutputAddress == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellRequestProcessingResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.GoldSellingReq, internalRequestId)
			;

			return await mutexBuilder.CriticalSection<BuySellRequestProcessingResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of `{ TextFormatter.FormatTokenAmount(amount, Common.Tokens.GOLD.Decimals) }` GOLD is `{transactionId}`");
					}
					catch { }

					try {

						var timeNow = DateTime.UtcNow;

						// ok
						if (request.TimeExpires > timeNow) {

							var suppRequest = new SellGoldCryptoSupportRequest() {

								Status = SupportRequestStatus.Pending,
								AmountWei = amount.ToString(),
								SellGoldRequestId = request.Id,
								TimeCreated = timeNow,

								UserId = request.UserId,
								OplogId = request.OplogId,
								RefUserFinHistoryId = request.RefUserFinHistoryId,
							};
							dbContext.SellGoldCryptoSupportRequest.Add(suppRequest);

							request.Status = SellGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. New support request #{suppRequest.Id} enqueued");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Success;
						}

						// expired
						else {

							request.Status = SellGoldRequestStatus.Expired;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Failed, $"Request #{request.Id} is expired");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Expired;
						}
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process sell request #{request.Id}");
					}
				}
				return BuySellRequestProcessingResult.MutexFailure;
			});
		}

		/// <summary>
		/// Process HW GOLD transferring transaction (core-worker queue)
		/// </summary>
		public static async Task<bool> ProcessTransferGoldHwTransaction(IServiceProvider services, long requestId) {

			// TODO: move to appconfig (debug/staging/prod)
			var confirmationsNeeded = 4;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.GoldHwTransTx, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.TransferGoldTransaction
						where
							r.Id == requestId &&
							(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.User)
						.Include(_ => _.RefUserFinHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
					;

					if (request == null) {
						return false;
					}

					try {

						// set next check time
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimeCreated, TimeSpan.FromSeconds(15), confirmationsNeeded);

						// initiate blockchain transaction
						if (request.Status == EthereumOperationStatus.Prepared) {

							var amount = BigInteger.Parse(request.AmountWei);
							var goldBalance = await ethereumReader.GetHotWalletGoldBalance(request.User.UserName);

							// valid?
							if (amount < 1 || amount > goldBalance) {

								request.Status = EthereumOperationStatus.Failed;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Failed, "Request failed. Invalid amount specified");
								}
								catch { }

								return false;
							}

							// update status to prevent double spending
							request.Status = EthereumOperationStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							// save eth transaction
							request.EthTransactionId = await ethereumWriter.TransferGoldFromHotWallet(
								toAddress: request.DestinationAddress,
								amount: amount,
								userId: request.User.UserName
							);
							request.RefUserFinHistory.RelEthTransactionId = request.EthTransactionId;

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = EthereumOperationStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == EthereumOperationStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId, confirmationsNeeded);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								var success = result == EthTransactionStatus.Success;

								request.Status = success ? EthereumOperationStatus.Success : EthereumOperationStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.RefUserFinHistory.Status = success ? UserFinHistoryStatus.Completed : UserFinHistoryStatus.Failed;
								request.RefUserFinHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == EthereumOperationStatus.Success) {
										await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (request.Status == EthereumOperationStatus.Failed) {
										await ticketDesk.UpdateTicket(request.OplogId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
									}
								}
								catch { }

								// TODO: failure logic?
							}

							return request.Status == EthereumOperationStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process hw transferring request #{request.Id}");
					}
				}

				return false;
			});
		}

		// ---

		public enum BuySellRequestProcessingResult {
			Success,
			Expired,
			InvalidArgs,
			NotFound,
			MutexFailure,
		}
	}
}
