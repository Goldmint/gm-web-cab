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

namespace Goldmint.CoreLogic.Finance.Fiat {

	public static class CryptoExchangeQueue {

		/// <summary>
		/// Estimate deposit operation value
		/// </summary>
		public static BigInteger ConvertAsset(CryptoExchangeAsset asset, bool amountIsToken, BigInteger amount, long priceCents) {
			if (amount < 0) {
				throw new Exception($"Amount cant be {amount}");
			}

			var ret = BigInteger.MinusOne;
			var decimals = 0;

			if (asset == CryptoExchangeAsset.ETH) {
				decimals = 18;
			}
			else {
				throw new NotImplementedException($"Estimation of {asset} is not implemented yet");
			}

			if (amountIsToken) {
				ret = amount * new BigInteger(priceCents) / BigInteger.Pow(10, decimals);
				if (ret > long.MaxValue) {
					throw new Exception($"Long-value cant handle {ret}");
				}
			}
			else {
				ret = amount * BigInteger.Pow(10, decimals) / new BigInteger(priceCents);
			}

			if (ret < 0) {
				throw new Exception($"Price cant be {ret}");
			}
			return ret;
		}

		/// <summary>
		/// Adjust exchange values right before actual processing
		/// </summary>
		private static Task<AdjustResult> AdjustExchangeRate(IServiceProvider services, bool isDeposit, CryptoExchangeAsset asset, FiatCurrency currency, long fixedRate) {

			if (fixedRate <= 0) {
				throw new ArgumentException("Illegal fixed token price");
			}

			// var appConfig = services.GetRequiredService<AppConfig>();
			// var rateProvider = services.GetRequiredService<ICryptoassetRateProvider>();

			var rate = fixedRate;
			var abort = false;

			// var currentRate = await rateProvider.GetRate(asset, currency);
			// var threshold = (long)(fixedRate * appConfig.Constants.ExchangeThreshold);
			// 
			// if (buying) {
			// 	if (currentRate < fixedRate - threshold) {
			// 		abort = true;
			// 	}
			// }
			// else {
			// 	if (currentRate > fixedRate + threshold) {
			// 		abort = true;
			// 	}
			// }

			return Task.FromResult(new AdjustResult() {
				RateCents = rate,
				Abort = abort,
			});
		}

		// ---

		/// <summary>
		/// Mark request as prepared for processing
		/// </summary>
		public static async Task<DepositWithdrawPreparationResult> PrepareDepositRequest(IServiceProvider services, CryptoExchangeAsset asset, long internalRequestId, string address, BigInteger amount, string transactionId) {

			if (internalRequestId <= 0) return DepositWithdrawPreparationResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return DepositWithdrawPreparationResult.InvalidArgs;
			if (amount < 0) return DepositWithdrawPreparationResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(transactionId)) return DepositWithdrawPreparationResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(CryptoExchangeQueue));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.CryptoDeposit
				where
					r.Origin == asset &&
					r.Id == internalRequestId &&
					r.Status == CryptoDepositStatus.Confirmed &&
					r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return DepositWithdrawPreparationResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CryptoDepositRequest, internalRequestId)
			;

			return await mutexBuilder.CriticalSection<DepositWithdrawPreparationResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return DepositWithdrawPreparationResult.NotFound;
					}

					try {

						if (amount.ToString().Length > 128) {
							throw new Exception($"Amount.ToString().Length exceeds db field size: {amount.ToString().Length}");
						}
						if (transactionId.Length > 256) {
							throw new Exception($"TransactionId.Length exceeds db field size: {transactionId.Length}");
						}

						request.Status = CryptoDepositStatus.Prepared;
						request.Amount = amount.ToString();
						request.TransactionId = transactionId;
						request.TimePrepared = DateTime.UtcNow;
						request.TimeNextCheck = DateTime.UtcNow;

						await dbContext.SaveChangesAsync();

						try {
							await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Request #{request.Id} marked for processing");
						}
						catch { }

						return DepositWithdrawPreparationResult.Success;
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to mark crypto-deposit request #{request.Id} for processing");
					}
				}
				return DepositWithdrawPreparationResult.MutexFailure;
			});
		}

		/// <summary>
		/// Process request on blockchain and check it
		/// </summary>
		public static async Task<bool> ProcessDepositReqeust(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(CryptoExchangeQueue));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.CryptoDepositRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.CryptoDeposit
						where
							r.Id == requestId &&
							r.Status == CryptoDepositStatus.Prepared
						select r
					)
						.AsTracking()
						.FirstOrDefaultAsync()
					;

					if (request == null) {
						return false;
					}

					try {

						var adjusted = await AdjustExchangeRate(
							services: services,
							isDeposit: true,
							asset: request.Origin,
							currency: request.Currency,
							fixedRate: request.RateCents
						);

						// oups
						if (adjusted.Abort) {
							request.Status = CryptoDepositStatus.Failed;
							request.TimeCompleted = DateTime.UtcNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"Cryptodeposit #{request.Id} cancelled due to significant price change. Cancelled automatically");
							}
							catch { }

							return false;
						}

						var amountCents = (long)ConvertAsset(
							asset: request.Origin,
							amountIsToken: true,
							amount: BigInteger.Parse(request.Amount),
							priceCents: adjusted.RateCents
						);

						try {
							await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Cryptodeposit #{request.Id} conversion: {request.Amount} {request.Origin.ToString()} => {TextFormatter.FormatAmount(amountCents, request.Currency)}");
						}
						catch { }

						if (amountCents <= 0) {
							logger.Error($"Failed to enqueue deposit from cryptodeposit #{request.Id}. Amount in cents is {amountCents}");
							return false;
						}

						// set next check time
						request.Status = CryptoDepositStatus.Processing;
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimePrepared ?? request.TimeCreated, TimeSpan.FromSeconds(30), 2);
						await dbContext.SaveChangesAsync();

						var depositResult = await DepositQueue.StartDepositFromCryptoDeposit(
							services: services,
							userId: request.UserId,
							cryptoDeposit: request,
							calculatedAmountCents: amountCents,
							financialHistoryId: request.RefFinancialHistoryId
						);

						if (depositResult.Error != null) {
							logger.Error(depositResult.Error, $"Failed to enqueue deposit from cryptodeposit #{request.Id}");
						}

						request.Status = depositResult.Status == FiatEnqueueResult.Success ? CryptoDepositStatus.Success : CryptoDepositStatus.Failed;
						request.TimeCompleted = DateTime.UtcNow;
						await dbContext.SaveChangesAsync();

						try {
							if (request.Status == CryptoDepositStatus.Success) {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"Cryptodeposit #{request.Id} marked as completed. Deposit enqueued");
							}
							else {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"Cryptodeposit #{request.Id} failed");
							}
						}
						catch { }
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process cryptodeposit request #{request.Id}");
					}
				}

				return false;
			});
		}

		// ---

		public class AdjustResult {

			/// <summary>
			/// Actual token rate to use
			/// </summary>
			public long RateCents { get; set; }

			/// <summary>
			/// Abort processing and cancel request
			/// </summary>
			public bool Abort { get; set; }
		}

		public enum DepositWithdrawPreparationResult {

			Success,
			InvalidArgs,
			NotFound,
			MutexFailure,
		}
	}
}
