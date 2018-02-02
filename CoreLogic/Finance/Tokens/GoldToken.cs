﻿using Goldmint.Common;
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

namespace Goldmint.CoreLogic.Finance.Tokens {

	public static class GoldToken {

		public static readonly int TokenPercision = 18;
		public static readonly long TokenPercisionMultiplier = (long)Math.Pow(10d, (double)TokenPercision);

		public static readonly int VisualTokenPercision = 6;
		public static readonly long VisualTokenPercisionMultiplier = (long)Math.Pow(10d, (double)VisualTokenPercision);

		// ---

		/// <summary>
		/// Token in wei. Ex: 1.5 => 1500000000000000000
		/// </summary>
		public static BigInteger ToWei(decimal amount) {

			if (amount <= 0) return BigInteger.Zero;

			var left = decimal.Truncate(amount);
			var right = decimal.Round(amount - left, TokenPercision) * TokenPercisionMultiplier;

			return 
				BigInteger.Multiply((BigInteger)left, (BigInteger)TokenPercisionMultiplier)
				+ (BigInteger)right
			;
		}

		/// <summary>
		/// Amount from wei. Ex: 1500000000000000000 => 1.5
		/// </summary>
		public static decimal FromWei(BigInteger amount) {

			if (amount <= 0) return 0m;

			if (amount > (BigInteger)decimal.MaxValue) {
				throw new ArgumentException("Too big value");
			}

			return (decimal)amount / TokenPercisionMultiplier;
		}

		/// <summary>
		/// Amount from wei. Ex: 1512345670000000000 => 1.512345
		/// </summary>
		public static double FromWeiFixed(BigInteger amount, bool roundUp) {
			if (amount <= 0) return 0d;

			var dec = FromWei(amount);

			var ret = 
				decimal.ToDouble(decimal.Round(dec * VisualTokenPercisionMultiplier, 1)) 
			;

			if (!roundUp) {
				ret = Math.Floor(ret);
			} else {
				ret = Math.Ceiling(ret);
			}

			return ret / VisualTokenPercisionMultiplier;
		}

		/// <summary>
		/// Estimate exchange operation
		/// </summary>
		public static Task<BuyingEstimationResult> EstimateBuying(long fiatAmountCents, long inputTotalVolumeCents, long pricePerGoldOunceCents) {

			if (pricePerGoldOunceCents <= 0) {
				throw new ArgumentException("Illegal gold price");
			}

			var min = 1L; // 1 cent
			var max = inputTotalVolumeCents;
			if (min > max) {
				min = 0;
				max = 0;
			}

			if (fiatAmountCents < min) fiatAmountCents = min;
			if (fiatAmountCents > max) fiatAmountCents = max;
			
			var goldAmount = ToWei(
				(fiatAmountCents / 100m) / (pricePerGoldOunceCents / 100m)
			);

			return Task.FromResult(
				new BuyingEstimationResult() {
					CentsUsed = fiatAmountCents,
					CentsMin = min,
					CentsMax = max,
					GoldAmount = goldAmount,
				}
			);
		}

		/// <summary>
		/// Estimate exchange operation
		/// </summary>
		public static Task<SellingEstimationResult> EstimateSelling(BigInteger goldAmountWei, BigInteger inputTotalVolumeWei, long pricePerGoldOunceCents) {

			if (pricePerGoldOunceCents <= 0) {
				throw new ArgumentException("Illegal gold price");
			}

			var min = ToWei(0.01M / (pricePerGoldOunceCents / 100m)); // gold amount for 1 cent
			var max = inputTotalVolumeWei;
			if (min > max) {
				min = 0;
				max = 0;
			}

			if (goldAmountWei < min) goldAmountWei = min;
			if (goldAmountWei > max) goldAmountWei = max;

			var fiatAmountCents = (long)decimal.Truncate(
				FromWei(goldAmountWei) * pricePerGoldOunceCents
			);

			return Task.FromResult(
				new SellingEstimationResult() {
					WeiUsed = goldAmountWei,
					WeiMin = min,
					WeiMax = max,
					CentsAmount = fiatAmountCents,
				}
			);
		}

		/// <summary>
		/// Adjust exchange values right before actual processing
		/// </summary>
		private static async Task<AdjustResult> AdjustExchangeValues(IServiceProvider services, bool buying, FiatCurrency currency, long fixedGoldRateCents) {
	
			if (fixedGoldRateCents <= 0) {
				throw new ArgumentException("Illegal fixed gold price");
			}

			var appConfig = services.GetRequiredService<AppConfig>();
			var goldRateProvider = services.GetRequiredService<IGoldRateProvider>();

			var rate = fixedGoldRateCents;
			var abort = false;

			var currentRate = await goldRateProvider.GetGoldRate(currency);
			var threshold = (long)(fixedGoldRateCents * appConfig.Constants.ExchangeThreshold);

			if (buying) {
				if (currentRate < fixedGoldRateCents - threshold) {
					abort = true;
				}
			}
			else {
				if (currentRate > fixedGoldRateCents + threshold) {
					abort = true;
				}
			}

			return new AdjustResult() {
				GoldRateCents = rate,
				Abort = abort,
			};
		}

		/// <summary>
		/// Mark request as prepared for processing
		/// </summary>
		public static async Task<bool> PrepareBuyingExchangeRequest(IServiceProvider services, long userId, string payload, string address, BigInteger requestIndex) {

			long payloadId = 0;
			if (!long.TryParse(payload, out payloadId) || payloadId <= 0) {
				return false;
			}

			if (userId <= 0) throw new ArgumentException("Used ID is invalid");
			if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is invalid");
			if (requestIndex < 0) throw new ArgumentException("Request index is invalid");

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.BuyRequest
				where r.Id == payloadId && r.UserId == userId && r.Status == ExchangeRequestStatus.Initial && r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return false;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.BuyRequest, payloadId)
			;

			return await mutexBuilder.LockAsync<bool>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return false;
					}

					try {
						request.Status = ExchangeRequestStatus.Processing;
						request.TimeRequested = DateTime.UtcNow;
						request.TimeNextCheck = DateTime.UtcNow;
						request.RequestIndex = requestIndex.ToString();

						await dbContext.SaveChangesAsync();

						try {
							await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Opened, "Request marked for processing");
						}
						catch { }

						return true;
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to mark buying request #{request.Id} for processing");
					}
					finally {
						dbContext.Detach(request);
					}
				}
				return false;
			});
		}

		/// <summary>
		/// Mark request as prepared for processing
		/// </summary>
		public static async Task<bool> PrepareSellingExchangeRequest(IServiceProvider services, long userId, string payload, string address, BigInteger requestIndex) {

			long payloadId = 0;
			if (!long.TryParse(payload, out payloadId) || payloadId <= 0) {
				return false;
			}

			if (userId <= 0) throw new ArgumentException("Used ID is invalid");
			if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is invalid");
			if (requestIndex < 0) throw new ArgumentException("Request index is invalid");

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var query =
				from r in dbContext.SellRequest
				where r.Id == payloadId && r.UserId == userId && r.Status == ExchangeRequestStatus.Initial && r.Address == address
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return false;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.SellRequest, payloadId)
			;

			return await mutexBuilder.LockAsync<bool>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).FirstOrDefaultAsync();
					if (request == null) {
						return false;
					}

					try {
						request.Status = ExchangeRequestStatus.Processing;
						request.TimeRequested = DateTime.UtcNow;
						request.TimeNextCheck = DateTime.UtcNow;
						request.RequestIndex = requestIndex.ToString();

						await dbContext.SaveChangesAsync();

						try {
							await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Opened, "Request marked for processing");
						}
						catch { }

						return true;
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to mark selling request #{request.Id} for processing");
					}
					finally {
						dbContext.Detach(request);
					}
				}
				return false;
			});
		}

		/// <summary>
		/// Process request on blockchain and check it
		/// </summary>
		public static async Task<bool> ProcessBuyingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.BuyRequest, requestId)
			;

			return await mutexBuilder.LockAsync(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.BuyRequest
						where
						r.Id == requestId &&
						(r.Status == Common.ExchangeRequestStatus.Processing || r.Status == Common.ExchangeRequestStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.FinancialHistory)
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
						if (request.Status == ExchangeRequestStatus.Processing) {

							// update status to prevent double spending
							request.Status = ExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Opened, "Blockchain transaction initiated");
							}
							catch { }

							var adjust = await AdjustExchangeValues(
								services: services,
								buying: true,
								currency: request.Currency,
								fixedGoldRateCents: request.FixedRateCents
							);

							// cancelled
							if (adjust.Abort) {

								request.Status = ExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Cancelled, "Request has not been cancelled. Possibly due to significant gold rate change");
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

							// set new status
							request.Status = ExchangeRequestStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Opened, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == ExchangeRequestStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId);

							// final
							if (result == BlockchainTransactionStatus.Success || result == BlockchainTransactionStatus.Failed) {

								var success = result == BlockchainTransactionStatus.Success;
								request.Status = success ? ExchangeRequestStatus.Success : ExchangeRequestStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.FinancialHistory.Status = success ? FinancialHistoryStatus.Success : FinancialHistoryStatus.Cancelled;
								request.FinancialHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == ExchangeRequestStatus.Success) {
										await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Success, "Request has been saved on blockchain");
									}
									if (request.Status == ExchangeRequestStatus.Failed) {
										await ticketDesk.UpdateGoldBuyingTicket(request.DeskTicketId, TicketStatus.Cancelled, "Request has not been saved on blockchain");
									}
								}
								catch { }
							}

							return request.Status == ExchangeRequestStatus.Success;
						}

					} catch (Exception e) {
						logger.Error(e, $"Failed to process buying request #{request.Id}");
					} finally {
						dbContext.Detach(request);
					}
				}

				return false;
			});
		}

		/// <summary>
		/// Post blockchain transaction for request
		/// </summary>
		public static async Task<bool> ProcessSellingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.SellRequest, requestId)
			;

			return await mutexBuilder.LockAsync(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.SellRequest
						where
						r.Id == requestId &&
						(r.Status == Common.ExchangeRequestStatus.Processing || r.Status == Common.ExchangeRequestStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.FinancialHistory)
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
						if (request.Status == ExchangeRequestStatus.Processing) {

							// update status to prevent double spending
							request.Status = ExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Opened, "Blockchain transaction initiated");
							}
							catch { }

							var adjust = await AdjustExchangeValues(
								services: services,
								buying: false,
								currency: request.Currency,
								fixedGoldRateCents: request.FixedRateCents
							);

							// cancelled
							if (adjust.Abort) {

								request.Status = ExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Cancelled, "Request has not been cancelled. Possibly due to significant gold rate change");
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

							// set new status
							request.Status = ExchangeRequestStatus.BlockchainConfirm;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Opened, "Blockchain transaction checking started");
							}
							catch { }

							return true;
						}

						if (request.Status == ExchangeRequestStatus.BlockchainConfirm) {

							var result = await ethereumReader.CheckTransaction(request.EthTransactionId);

							// final
							if (result == BlockchainTransactionStatus.Success || result == BlockchainTransactionStatus.Failed) {

								var success = result == BlockchainTransactionStatus.Success;

								request.Status = success ? ExchangeRequestStatus.Success : ExchangeRequestStatus.Failed;
								request.TimeCompleted = DateTime.UtcNow;

								request.FinancialHistory.Status = success ? FinancialHistoryStatus.Success : FinancialHistoryStatus.Cancelled;
								request.FinancialHistory.TimeCompleted = request.TimeCompleted;

								await dbContext.SaveChangesAsync();

								try {
									if (request.Status == ExchangeRequestStatus.Success) {
										await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Success, "Request has been saved on blockchain");
									}
									if (request.Status == ExchangeRequestStatus.Failed) {
										await ticketDesk.UpdateGoldSellingTicket(request.DeskTicketId, TicketStatus.Cancelled, "Request has not been saved on blockchain");
									}
								}
								catch { }
							}

							return request.Status == ExchangeRequestStatus.Success;
						}

					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process selling request #{request.Id}");
					}
					finally {
						dbContext.Detach(request);
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
			public long CentsUsed { get; set; }

			/// <summary>
			/// Input value lower limit
			/// </summary>
			public long CentsMin { get; set; }

			/// <summary>
			/// Input value upper limit
			/// </summary>
			public long CentsMax { get; set; }

			/// <summary>
			/// Result gold tokens
			/// </summary>
			public BigInteger GoldAmount { get; set; }

		}

		public class SellingEstimationResult {

			/// <summary>
			/// Input value used in estimation
			/// </summary>
			public BigInteger WeiUsed { get; set; }

			/// <summary>
			/// Input value lower limit
			/// </summary>
			public BigInteger WeiMin { get; set; }

			/// <summary>
			/// Input value upper limit
			/// </summary>
			public BigInteger WeiMax { get; set; }

			/// <summary>
			/// Result cents amount
			/// </summary>
			public long CentsAmount { get; set; }

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
	}
}
