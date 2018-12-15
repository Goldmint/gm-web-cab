using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Finance {

	public static class EthereumContract {

		/// <summary>
		/// Process Ethereum operation (core-worker queue)
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
						.Include(_ => _.RelUserFinHistory)
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
						op.TimeNextCheck = DateTime.UtcNow.AddSeconds(60);

						// initiate blockchain transaction
						if (op.Status == EthereumOperationStatus.Prepared) {
							IEthereumOperation processor = null;

							switch (op.Type) {
								case EthereumOperationType.TransferGoldFromHw:
									processor = new TransferOperation();
									break;
								case EthereumOperationType.ContractProcessBuyRequestEth:
								case EthereumOperationType.ContractProcessSellRequestEth:
									processor = new ProcessRequestOperation();
									break;
								case EthereumOperationType.ContractCancelBuyRequest:
								case EthereumOperationType.ContractCancelSellRequest:
									processor = new CancelRequestOperation();
									break;
								case EthereumOperationType.ContractProcessBuyRequestFiat:
									processor = new ProcessBuyRequestFiatOperation();
									break;
								case EthereumOperationType.ContractProcessSellRequestFiat:
									processor = new ProcessSellRequestFiatOperation();
									break;
								case EthereumOperationType.SendBuyingSupportEther:
									processor = new SendBuyingSupportEtherOperation();
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
							op.RelUserFinHistory.RelEthTransactionId = op.EthTransactionId;
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
							
							// own scope
							try {
								using (var scopedServices = services.CreateScope()) {
									await GoldToken.OnEthereumOperationConfirmationStarted(scopedServices.ServiceProvider, op);
								}
							}
							catch (Exception e) {
								logger.Error(e);
							}

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
								}

								// own scope
								try { 
									using (var scopedServices = services.CreateScope()) {
										await GoldToken.OnEthereumOperationResult(scopedServices.ServiceProvider, op);
									}
								}
								catch (Exception e) {
									logger.Error(e);
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
					userAddress: op.DestinationAddress,
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
		/// Buy/sell-for-ETH request processing
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

				if (op.Discount < 0 || op.Discount > 100) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid discount percentage",
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
				var discount = ((decimal)op.Discount).ToEther();

				var txid = await ethereumWriter.ProcessRequestEth(
					requestIndex: reqIndex,
					ethPerGold: rate,
					discountPercentage: discount
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

				var txid = await ethereumWriter.CancelRequest(
					requestIndex: reqIndex
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}

		/// <summary>
		/// Buy-for-fiat request processing
		/// </summary>
		internal sealed class ProcessBuyRequestFiatOperation : IEthereumOperation {

			public Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {

				if (op.RelatedExchangeRequestId == null || op.RelatedExchangeRequestId < 1) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Request ID must be a valid ID",
					});
				}

				if (op.CentsAmount == null || op.CentsAmount < 1) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Request cents amount is invalid",
					});
				}

				if (!long.TryParse(op.Rate ?? "-1", out var rate) || rate < 0) {
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

				var reference = op.RelatedExchangeRequestId ?? 0;
				var centsAmount = op.CentsAmount ?? 0;
				var rate = long.Parse(op.Rate);

				var txid = await ethereumWriter.ProcessBuyRequestFiat(
					userId: op.User.UserName,
					reference: reference,
					userAddress: op.DestinationAddress,
					amountCents: centsAmount,
					centsPerGold: rate
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}

		/// <summary>
		/// Sell-for-fiat request processing
		/// </summary>
		internal sealed class ProcessSellRequestFiatOperation : IEthereumOperation {

			public Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {

				if (!BigInteger.TryParse(op.EthRequestIndex ?? "-1", out var index) || index < 0) {
					return Task.FromResult(new CheckResult() {
						Success = false,
						TicketErrorDesc = "Invalid request index format",
					});
				}

				if (!long.TryParse(op.Rate ?? "-1", out var rate) || rate < 0) {
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
				var rate = long.Parse(op.Rate);

				var txid = await ethereumWriter.ProcessSellRequestFiat(
					requestIndex: reqIndex,
					centsPerGold: rate
				);

				return new ExecResult() {
					Success = txid != null,
					TxId = txid,
				};
			}
		}
		
		/// <summary>
		/// Send ether to the address
		/// </summary>
		internal sealed class SendBuyingSupportEtherOperation : IEthereumOperation {

			public Task<CheckResult> Check(IServiceProvider services, DAL.Models.EthereumOperation op) {
				return Task.FromResult(new CheckResult() {
					Success = true,
				});
			}

			public async Task<ExecResult> Exec(IServiceProvider services, DAL.Models.EthereumOperation op) {

				var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
				var rcfgHolder = services.GetRequiredService<RuntimeConfigHolder>();
				var rcfg = rcfgHolder.Clone();

				string txid = null;

				// check one more time
				if (rcfg.Gold.SupportingEther.Enable && rcfg.Gold.SupportingEther.EtherToSend > 0) {
					var amount = BigInteger.Zero;
					try {
						amount = new BigInteger(
							decimal.Floor(
								(decimal)BigInteger.Pow(10, TokensPrecision.Ethereum) * (decimal)rcfg.Gold.SupportingEther.EtherToSend
							)
						);
					}
					catch {
					}

					if (amount > 0) {
						txid = await ethereumWriter.TransferEther(
							op.DestinationAddress,
							amount
						);
					}
				}

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
