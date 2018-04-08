using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL.Models;

namespace Goldmint.CoreLogic.Finance {

	public static class GoldToken {

		public static Task<BigInteger> EstimateBuying(BigInteger amount, CryptoCurrency cur, long fixedGoldRate, FiatCurrency exchangeCurrency, long fixedCurrencyRate) {

			// TODO: assert: amount <= 0
			// TODO: safe rates
			// TODO: compare actual and fixed rates
			// TODO: fees

			var curDecimals = 0;

			if (cur == CryptoCurrency.ETH) {
				curDecimals = Common.Tokens.ETH.Decimals;
			}
			else {
				throw new NotImplementedException($"Estimation is not implemented for {cur.ToString()}");
			}

			// var goldRate = await GoldRate.GetGoldRate(exchangeCurrency);

			var exchangeAmount = amount * new BigInteger(fixedCurrencyRate);
			var goldAmount = exchangeAmount * BigInteger.Pow(10, Common.Tokens.GOLD.Decimals) / fixedGoldRate / BigInteger.Pow(10, curDecimals);

			return Task.FromResult(goldAmount);
		}

		private static IssueGoldTransaction MakeGoldIssuingTransaction(long userId, long userFinHistoryId, string deskTicketId, string address, BigInteger amount, IssueGoldOrigin origin, long originId) {

			var timeNow = DateTime.UtcNow;

			return new IssueGoldTransaction() {

				Status = EthereumOperationStatus.Initial,

				DestinationAddress = address,
				Amount = amount.ToString(),
				DeskTicketId = deskTicketId,

				Origin = origin,
				OriginId = originId,

				TimeCreated = timeNow,
				TimeNextCheck = timeNow,

				RefUserFinHistoryId = userFinHistoryId,
				UserId = userId,
			};
		}

		// ---

		/// <summary>
		/// Process GOLD buying request (core-worker harvester)
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> ProcessBuyRequest(IServiceProvider services, long internalRequestId, string address, BigInteger amount, string transactionId) {

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
					r.Input == BuyGoldRequestInput.EthereumContractPayment &&
					r.Id == internalRequestId &&
					r.Status == BuyGoldRequestStatus.Confirmed &&
					r.Destination == BuyGoldRequestDestination.EthereumAddress &&
					r.DestinationAddress == address
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
						await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"User's Ethereum transaction of `{ TextFormatter.FormatTokenAmount(amount, 18) }` ETH is `{transactionId}`");
					}
					catch { }

					try {

						var timeNow = DateTime.UtcNow;

						// estimate
						var goldAmount = await EstimateBuying(
							amount: amount,
							cur: CryptoCurrency.ETH,
							fixedGoldRate: request.GoldRateCents,
							exchangeCurrency: request.ExchangeCurrency,
							fixedCurrencyRate: request.InputRateCents
						);

						// ok
						if (request.TimeExpires > timeNow) {

							var issTx = MakeGoldIssuingTransaction(
								userId: request.UserId,
								userFinHistoryId: request.RefUserFinHistoryId,
								deskTicketId: request.DeskTicketId,
								address: request.DestinationAddress,
								amount: goldAmount,
								origin: IssueGoldOrigin.BuyingRequest,
								originId: request.Id
							);
							dbContext.IssueGoldTransaction.Add(issTx);

							request.Status = BuyGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. New GOLD issuing transaction #{issTx.Id} enqueued");
							}
							catch {
							}

							// ready to issue
							issTx.Status = EthereumOperationStatus.Prepared;
							issTx.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							return BuySellRequestProcessingResult.Success;
						}

						// expired
						else {
							request.Status = BuyGoldRequestStatus.Failed;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed,
									$"Request #{request.Id} is expired");
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
							var goldBalance = await ethereumReader.GetUserGoldBalance(request.User.UserName);

							// valid?
							if (amount < 1 || amount > goldBalance) {

								request.Status = EthereumOperationStatus.Failed;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request failed. Invalid amount specified");
								}
								catch { }

								return false;
							}

							// update status to prevent double spending
							request.Status = EthereumOperationStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
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
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = EthereumOperationStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
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
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (request.Status == EthereumOperationStatus.Failed) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
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

		/// <summary>
		/// Process GOLD issuing transaction (core-worker queue)
		/// </summary>
		public static async Task<bool> ProcessIssuingTransaction(IServiceProvider services, long requestId) {

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
				.Mutex(MutexEntity.GoldIssuingTx, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.IssueGoldTransaction
						where
							r.Id == requestId &&
							(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm)
						select r
					)
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

							var amount = BigInteger.Parse(request.Amount);

							// valid?
							if (amount < 1) {

								request.Status = EthereumOperationStatus.Failed;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Transaction failed. Invalid amount specified");
								}
								catch { }

								return false;
							}

							// update status to prevent double spending
							request.Status = EthereumOperationStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							// save eth transaction
							// TODO: issue
							request.EthTransactionId = "0x0";
							request.RefUserFinHistory.RelEthTransactionId = request.EthTransactionId;

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = EthereumOperationStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
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
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Completed, "Transaction has been saved on blockchain");
									}
									if (request.Status == EthereumOperationStatus.Failed) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Transaction has NOT been saved on blockchain");
									}
								}
								catch { }

								// TODO: failure logic?
							}

							return request.Status == EthereumOperationStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process issuing transaction #{request.Id}");
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

		/*
		
		// ---

		/// <summary>
		/// Mark request as prepared for processing
		/// </summary>
		public static async Task<BuySellPreparationResult> PrepareEthBuyingRequest(IServiceProvider services, long userId, string payload, string address, BigInteger requestIndex) {

			if (!long.TryParse(payload, out var internalRequestId) || internalRequestId <= 0) {
				return BuySellPreparationResult.InvalidArgs;
			}

			if (userId <= 0) return BuySellPreparationResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellPreparationResult.InvalidArgs;
			if (requestIndex < 0) return BuySellPreparationResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.BuyRequest
				where
					r.Type == GoldExchangeRequestType.EthRequest &&
					r.Id == internalRequestId &&
					r.UserId == userId &&
					r.Status == GoldExchangeRequestStatus.Confirmed &&
					r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellPreparationResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.EthBuyRequest, internalRequestId)
			;

			return await mutexBuilder.CriticalSection<BuySellPreparationResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellPreparationResult.NotFound;
					}

					try {
						request.Status = GoldExchangeRequestStatus.Prepared;
						request.TimeRequested = DateTime.UtcNow;
						request.TimeNextCheck = DateTime.UtcNow;
						request.RequestIndex = requestIndex.ToString();

						await dbContext.SaveChangesAsync();

						try {
							await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Request #{request.Id} marked for processing");
						}
						catch { }

						return BuySellPreparationResult.Success;
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to mark buying request #{request.Id} for processing");
					}
				}
				return BuySellPreparationResult.MutexFailure;
			});
		}

		/// <summary>
		/// Mark request as prepared for processing
		/// </summary>
		public static async Task<BuySellPreparationResult> PrepareEthSellingRequest(IServiceProvider services, long userId, string payload, string address, BigInteger requestIndex) {

			if (!long.TryParse(payload, out var internalRequestId) || internalRequestId <= 0) {
				return BuySellPreparationResult.InvalidArgs;
			}

			if (userId <= 0) return BuySellPreparationResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellPreparationResult.InvalidArgs;
			if (requestIndex < 0) return BuySellPreparationResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.SellRequest
				where
					r.Type == GoldExchangeRequestType.EthRequest &&
					r.Id == internalRequestId &&
					r.UserId == userId &&
					r.Status == GoldExchangeRequestStatus.Confirmed &&
					r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellPreparationResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.EthSellRequest, internalRequestId)
			;

			return await mutexBuilder.CriticalSection<BuySellPreparationResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellPreparationResult.NotFound;
					}

					try {
						request.Status = GoldExchangeRequestStatus.Prepared;
						request.TimeRequested = DateTime.UtcNow;
						request.TimeNextCheck = DateTime.UtcNow;
						request.RequestIndex = requestIndex.ToString();

						await dbContext.SaveChangesAsync();

						try {
							await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Request #{request.Id} marked for processing");
						}
						catch { }

						return BuySellPreparationResult.Success;
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to mark selling request #{request.Id} for processing");
					}
				}
				return BuySellPreparationResult.MutexFailure;
			});
		}

		/// <summary>
		/// Post blockchain transaction for request
		/// </summary>
		public static async Task<bool> ProcessEthSellingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.EthSellRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.SellRequest
						where
						r.Id == requestId &&
						r.Type == GoldExchangeRequestType.EthRequest &&
						(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.RefUserFinHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
					;

					if (request == null) {
						return false;
					}

					try {

						var requestIndex = BigInteger.Parse(request.RequestIndex);

						// set next check time
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimeRequested ?? request.TimeCreated, TimeSpan.FromSeconds(10), 4);

						// initiate blockchain transaction
						if (request.Status == GoldExchangeRequestStatus.Prepared) {

							// update status to prevent double spending
							request.Status = GoldExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							var adjust = await AdjustExchangeGoldRate(
								services: services,
								buying: false,
								currency: request.Currency,
								fixedGoldRateCents: request.FixedRateCents
							);

							// cancelled
							if (adjust.Abort) {

								request.Status = GoldExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has been cancelled. Possibly due to significant gold rate change");
								}
								catch { }

								// post
								await ethereumWriter.CancelExchangeRequest(requestIndex);

								return false;
							}

							// save actual rate
							request.ActualRateCents = adjust.GoldRateCents;

							// save eth transaction
							request.EthTransactionId = await ethereumWriter.ProcessExchangeRequest(
								requestIndex: requestIndex,
								currency: request.Currency,
								amountCents: request.FiatAmountCents,
								centsPerGoldToken: adjust.GoldRateCents
							);
							request.RefUserFinHistory.RelEthTransactionId = request.EthTransactionId;

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = GoldExchangeRequestStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == GoldExchangeRequestStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								var success = result == EthTransactionStatus.Success;

								request.Status = success ? GoldExchangeRequestStatus.Success : GoldExchangeRequestStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.RefUserFinHistory.Status = success ? UserFinHistoryStatus.Completed : UserFinHistoryStatus.Failed;
								request.RefUserFinHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == GoldExchangeRequestStatus.Success) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (request.Status == GoldExchangeRequestStatus.Failed) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
									}
								}
								catch { }
							}

							return request.Status == GoldExchangeRequestStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process selling request #{request.Id}");
					}
				}

				return false;
			});
		}

		// ---

		/// <summary>
		/// Process request on blockchain and check it
		/// </summary>
		public static async Task<bool> ProcessHwBuyingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.HWBuyRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.BuyRequest
						where
							r.Type == GoldExchangeRequestType.HWRequest &&
							r.Id == requestId &&
							(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm)
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
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimeRequested ?? request.TimeCreated, TimeSpan.FromSeconds(10), 4);

						// initiate blockchain transaction
						if (request.Status == GoldExchangeRequestStatus.Prepared) {

							// update status to prevent double spending
							request.Status = GoldExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							var adjust = await AdjustExchangeGoldRate(
								services: services,
								buying: true,
								currency: request.Currency,
								fixedGoldRateCents: request.FixedRateCents
							);

							// cancelled
							if (adjust.Abort) {

								request.Status = GoldExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has been cancelled. Possibly due to significant gold rate change");
								}
								catch { }

								return false;
							}

							// save actual rate
							request.ActualRateCents = adjust.GoldRateCents;

							// save eth transaction
							request.EthTransactionId = await ethereumWriter.ProcessHotWalletExchangeRequest(
								userId: request.User.UserName,
								isBuying: true,
								currency: request.Currency,
								amountCents: request.FiatAmountCents,
								centsPerGoldToken: adjust.GoldRateCents
							);
							request.RefUserFinHistory.RelEthTransactionId = request.EthTransactionId;

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = GoldExchangeRequestStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == GoldExchangeRequestStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								var success = result == EthTransactionStatus.Success;
								request.Status = success ? GoldExchangeRequestStatus.Success : GoldExchangeRequestStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.RefUserFinHistory.Status = success ? UserFinHistoryStatus.Completed : UserFinHistoryStatus.Failed;
								request.RefUserFinHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == GoldExchangeRequestStatus.Success) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (request.Status == GoldExchangeRequestStatus.Failed) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
									}
								}
								catch { }
							}

							return request.Status == GoldExchangeRequestStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process buying request #{request.Id}");
					}
				}

				return false;
			});
		}

		/// <summary>
		/// Post blockchain transaction for request
		/// </summary>
		public static async Task<bool> ProcessHwSellingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.HWSellRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.SellRequest
						where
							r.Id == requestId &&
							r.Type == GoldExchangeRequestType.HWRequest &&
							(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm)
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
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimeRequested ?? request.TimeCreated, TimeSpan.FromSeconds(10), 4);

						// initiate blockchain transaction
						if (request.Status == GoldExchangeRequestStatus.Prepared) {

							// update status to prevent double spending
							request.Status = GoldExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							var adjust = await AdjustExchangeGoldRate(
								services: services,
								buying: false,
								currency: request.Currency,
								fixedGoldRateCents: request.FixedRateCents
							);

							// cancelled
							if (adjust.Abort) {

								request.Status = GoldExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has been cancelled. Possibly due to significant gold rate change");
								}
								catch { }

								return false;
							}

							// save actual rate
							request.ActualRateCents = adjust.GoldRateCents;

							// save eth transaction
							request.EthTransactionId = await ethereumWriter.ProcessHotWalletExchangeRequest(
								userId: request.User.UserName,
								isBuying: false,
								currency: request.Currency,
								amountCents: request.FiatAmountCents,
								centsPerGoldToken: adjust.GoldRateCents
							);
							request.RefUserFinHistory.RelEthTransactionId = request.EthTransactionId;

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Blockchain transaction is {request.EthTransactionId}");
							}
							catch { }

							// set new status
							request.Status = GoldExchangeRequestStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == GoldExchangeRequestStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId);

							// final
							if (result == EthTransactionStatus.Success || result == EthTransactionStatus.Failed) {

								var success = result == EthTransactionStatus.Success;

								request.Status = success ? GoldExchangeRequestStatus.Success : GoldExchangeRequestStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.RefUserFinHistory.Status = success ? UserFinHistoryStatus.Completed : UserFinHistoryStatus.Failed;
								request.RefUserFinHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == GoldExchangeRequestStatus.Success) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (request.Status == GoldExchangeRequestStatus.Failed) {
										await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
									}
								}
								catch { }
							}

							return request.Status == GoldExchangeRequestStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process hw selling request #{request.Id}");
					}
				}

				return false;
			});
		}

		// ---

		public class BuyingEstimationResult {

			/// <summary>
			/// Input value used in estimation
			/// </summary>
			public long InputUsed { get; set; }

			/// <summary>
			/// Input value lower limit
			/// </summary>
			public long InputMin { get; set; }

			/// <summary>
			/// Input value upper limit
			/// </summary>
			public long InputMax { get; set; }

			/// <summary>
			/// Result gold tokens
			/// </summary>
			public BigInteger ResultGold { get; set; }

			/// <summary>
			/// Result cents fee amount
			/// </summary>
			public long ResultFeeCents { get; set; }

			/// <summary>
			/// Result net cents amount
			/// </summary>
			public long ResultNetCents { get; set; }

		}

		public class SellingEstimationResult {

			/// <summary>
			/// Input value used in estimation
			/// </summary>
			public BigInteger InputUsed { get; set; }

			/// <summary>
			/// Input value lower limit
			/// </summary>
			public BigInteger InputMin { get; set; }

			/// <summary>
			/// Input value upper limit
			/// </summary>
			public BigInteger InputMax { get; set; }

			/// <summary>
			/// Result cents amount
			/// </summary>
			public long ResultGrossCents { get; set; }

			/// <summary>
			/// Result cents fee amount
			/// </summary>
			public long ResultFeeCents { get; set; }

			/// <summary>
			/// Result net cents amount
			/// </summary>
			public long ResultNetCents { get; set; }
		}

		public class AdjustResult {

			/// <summary>
			/// Actual gold rate to use
			/// </summary>
			public long GoldRateCents { get; set; }

			/// <summary>
			/// Abort processing and cancel request
			/// </summary>
			public bool Abort { get; set; }
		}
		*/
	}
}
