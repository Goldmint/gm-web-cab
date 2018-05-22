using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using NLog;

namespace Goldmint.CoreLogic.Finance {

	public static class EthereumContract {

		/// <summary>
		/// Process HW GOLD transferring transaction (core-worker queue)
		/// </summary>
		public static async Task<bool> ExecuteOperation(IServiceProvider services, long operationId, int confirmationsRequired) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.EthOperation, operationId)
			;

			logger.Trace($"Locking #{operationId}");

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					logger.Trace($"Locked #{operationId}");

					var op = await (
						from r in dbContext.EthereumOperation
						where
							r.Id == operationId &&
							(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.User)
						.Include(_ => _.RefUserFinHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
					;

					if (op == null) {
						logger.Warn($"Entry not found for #{operationId}");
						return false;
					}

					logger.Trace($"Entry found for #{operationId}, current status is {op.Status.ToString()}");

					try {

						// set next check time
						op.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(op.TimeCreated, TimeSpan.FromSeconds(15), confirmationsRequired);

						// initiate blockchain transaction
						if (op.Status == EthereumOperationStatus.Prepared) {
							IEthereumOperation processor = null;

							switch (op.Type) {
								case EthereumOperationType.TransferGoldFromHw:
									processor = new TransferOperation();
									break;
								case EthereumOperationType.ContractProcessBuyRequest:
								case EthereumOperationType.ContractProcessSellRequest:
									processor = new ProcessRequestOperation();
									break;
								case EthereumOperationType.ContractCancelBuyRequest:
								case EthereumOperationType.ContractCancelSellRequest:
									processor = new CancelRequestOperation();
									break;
								default:
									logger.Error($"Ethereum contract processor is not implemented for type {op.Type.ToString()} of operation #{op.Id}");

									FinalizeOperation(op, logger, false);
									await dbContext.SaveChangesAsync();
									try {
										await ticketDesk.Update(op.OplogId, UserOpLogStatus.Failed, "Ethereum contract unknown operation");
									}
									catch { }
									return false;
							}

							// check
							logger.Trace($"Checking #{operationId}");
							var procCheck = await processor.Check(services, op);
							if (!procCheck.Success) {
								logger.Warn($"Check failed for #{operationId}: " + procCheck.TicketErrorDesc);

								FinalizeOperation(op, logger, false);
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(op.OplogId, UserOpLogStatus.Failed, "Failure while checking operation:" + procCheck.TicketErrorDesc);
								}
								catch { }

								return false;
							}

							// update status to prevent double spending
							op.Status = EthereumOperationStatus.BlockchainInit;
							logger.Trace($"Changing status to {op.Status.ToString()} for #{operationId}");
							await dbContext.SaveChangesAsync();
							try {
								await ticketDesk.Update(op.OplogId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							// exec
							logger.Trace($"Executing #{operationId}");
							var procExec = await processor.Exec(services, op);
							if (!procExec.Success) {
								logger.Warn($"Exec failed for #{operationId} (out of gas?)");

								FinalizeOperation(op, logger, false);
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(op.OplogId, UserOpLogStatus.Failed, "Failure while executing operation. See logs");
								}
								catch { }

								return false;
							}

							logger.Trace($"Exec success for #{operationId}, txid {procExec.TxId}");

							// save eth transaction
							op.EthTransactionId = procExec.TxId;
							op.RefUserFinHistory.RelEthTransactionId = op.EthTransactionId;
							try {
								await ticketDesk.Update(op.OplogId, UserOpLogStatus.Pending, $"Blockchain transaction is {op.EthTransactionId}");
							}
							catch { }

							op.Status = EthereumOperationStatus.BlockchainConfirm;
							logger.Trace($"Changing status to {op.Status.ToString()} for #{operationId}");
							await dbContext.SaveChangesAsync();
							try {
								await ticketDesk.Update(op.OplogId, UserOpLogStatus.Pending, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (op.Status == EthereumOperationStatus.BlockchainConfirm) {

							logger.Trace($"Checking tx info for #{operationId}");
							var txInfo = await ethereumReader.CheckTransaction(op.EthTransactionId, confirmationsRequired);

							// final
							if (txInfo.Status == EthTransactionStatus.Success || txInfo.Status == EthTransactionStatus.Failed) {

								logger.Trace($"Tx status is final for #{operationId}: {txInfo.Status.ToString()}");

								var success = txInfo.Status == EthTransactionStatus.Success;

								FinalizeOperation(op, logger, success);

								// request cancellation => finhistory should fail anyway
								if (op.Type == EthereumOperationType.ContractCancelBuyRequest) {
									op.RefUserFinHistory.Status = UserFinHistoryStatus.Failed;
								}

								await dbContext.SaveChangesAsync();

								try {
									if (op.Status == EthereumOperationStatus.Success) {
										await ticketDesk.Update(op.OplogId, UserOpLogStatus.Completed, "Request has been saved on blockchain");
									}
									if (op.Status == EthereumOperationStatus.Failed) {
										await ticketDesk.Update(op.OplogId, UserOpLogStatus.Failed, "Request has NOT been saved on blockchain");
									}
								}
								catch { }

								if (!success) {
									logger.Warn($"Operation #{operationId} failed");

									// TODO: failure logic?
								}
							}

							return op.Status == EthereumOperationStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process #{op.Id}");

						// TODO: alert
					}
				}
				else {
					logger.Warn($"Failed to lock #{operationId}");
				}

				return false;
			});
		}

		private static void FinalizeOperation(DAL.Models.EthereumOperation op, ILogger logger, bool success) {

			op.Status = success ? EthereumOperationStatus.Success : EthereumOperationStatus.Failed;
			op.TimeCompleted = DateTime.UtcNow;

			op.RefUserFinHistory.Status = success ? UserFinHistoryStatus.Completed : UserFinHistoryStatus.Failed;
			op.RefUserFinHistory.TimeCompleted = op.TimeCompleted;

			logger.Trace($"Changing status to {op.Status.ToString()} for #{op.Id}");
		}

		// ---

		private interface IEthereumOperation {

			Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op);
			Task<ExecResult> Exec(IServiceProvider services, DAL.Models.EthereumOperation op);
		}

		/// <summary>
		/// GOLD from HW transferring
		/// </summary>
		internal sealed class TransferOperation : IEthereumOperation {

			public async Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {

				var ethereumReader = services.GetRequiredService<IEthereumReader>();

				if (!BigInteger.TryParse(op.GoldAmount ?? "-1", out var amount) || amount < 1) {
					return new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid amount format",
					};
				}

				var goldBalance = await ethereumReader.GetHotWalletGoldBalance(op.User.UserName);

				// valid?
				if (amount < 1 || amount > goldBalance) {
					return new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid amount specified",
					};
				}

				return new CheckResult() {
					Success = true,
				};
			}

			public async Task<ExecResult> Exec(IServiceProvider services, DAL.Models.EthereumOperation op) {

				var ethereumWriter = services.GetRequiredService<IEthereumWriter>();

				var amount = BigInteger.Parse(op.GoldAmount);

				var txid = await ethereumWriter.TransferGoldFromHotWallet(
					toAddress: op.DestinationAddress,
					amount: amount,
					userId: op.User.UserName
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}

		/// <summary>
		/// Buy/sell request processing
		/// </summary>
		internal sealed class ProcessRequestOperation : IEthereumOperation {

			public Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {

				if (!BigInteger.TryParse(op.EthRequestIndex ?? "-1", out var index) || index < 0) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid request index format",
					});
				}

				if (!BigInteger.TryParse(op.Rate ?? "-1", out var rate) || rate < 0) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid rate format",
					});
				}

				return Task.FromResult(new CheckResult() {
					Success = true,
				});
			}

			public async Task<ExecResult> Exec(IServiceProvider services, DAL.Models.EthereumOperation op) {

				var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
				var ticketDesk = services.GetRequiredService<IOplogProvider>();

				var reqIndex = BigInteger.Parse(op.EthRequestIndex);
				var rate = BigInteger.Parse(op.Rate);

				var txid = await ethereumWriter.ProcessBuySellRequest(
					requestIndex: reqIndex,
					ethPerGold: rate
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}

		/// <summary>
		/// Buy/sell request cancellation
		/// </summary>
		internal sealed class CancelRequestOperation : IEthereumOperation {

			public Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {

				if (!BigInteger.TryParse(op.EthRequestIndex ?? "-1", out var index) || index < 0) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid request index format",
					});
				}

				return Task.FromResult(new CheckResult() {
					Success = true,
				});
			}

			public async Task<ExecResult> Exec(IServiceProvider services, DAL.Models.EthereumOperation op) {

				var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
				var ticketDesk = services.GetRequiredService<IOplogProvider>();

				var reqIndex = BigInteger.Parse(op.EthRequestIndex);

				var txid = await ethereumWriter.CancelBuySellRequest(
					requestIndex: reqIndex
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}

		// ---

		internal class CheckResult {

			public bool Success { get; internal set; }
			public string TicketErrorDesc { get; internal set; }
		}

		internal class ExecResult {

			public bool Success { get; internal set; }
			public string TxId { get; internal set; }
		}
	}
}
