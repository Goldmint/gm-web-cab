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
		public static readonly decimal TokenPercisionMultiplier = (decimal)Math.Pow(10d, (double)TokenPercision);

		// ---

		/// <summary>
		/// Token in wei. Ex: 1.5 => 1500000000000000000
		/// </summary>
		public static BigInteger ToWei(decimal amount) {
			if (amount <= 0) return BigInteger.Zero;
			var str = amount.ToString("F" + (TokenPercision + 1), System.Globalization.CultureInfo.InvariantCulture);
			var parts = str.Substring(0, str.Length - 1).Split('.');
			var left = parts.ElementAtOrDefault(0);
			var right = (parts.ElementAtOrDefault(1) ?? "0");
			return BigInteger.Parse(left + right);
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
		/// Amount from wei. Ex: 1512345670000000000 => 1.51234567
		/// </summary>
		public static string FromWeiFixed(BigInteger amount) {
			if (amount <= 0) return "0";

			var str = amount.ToString().PadLeft(TokenPercision + 1, '0');
			str = str.Substring(0, str.Length - TokenPercision) + "." + str.Substring(str.Length - 18);

			return str.TrimEnd('0', '.');
		}

		/// <summary>
		/// Estimate exchange operation
		/// </summary>
		public static Task<BuyingEstimationResult> EstimateBuying(long fiatAmountCents, long fiatTotalVolumeCents, long pricePerGoldOunceCents, BigInteger mntpBalance) {

			if (pricePerGoldOunceCents <= 0) {
				throw new ArgumentException("Illegal gold price");
			}

			var min = 100L; // 100 cents
			var max = fiatTotalVolumeCents;
			if (min > max) {
				min = 0;
				max = 0;
			}

			if (fiatAmountCents < min) fiatAmountCents = min;
			if (fiatAmountCents > max) fiatAmountCents = max;

			var fiatFeeCents = MntpToken.getBuyingFee(mntpBalance, fiatAmountCents);
			var fiatCents = Math.Max(0L, fiatAmountCents - fiatFeeCents);

			var goldAmount = ToWei((fiatCents / 100M) / (pricePerGoldOunceCents / 100M));

			return Task.FromResult(
				new BuyingEstimationResult() {
					InputUsed = fiatAmountCents,
					InputMin = min,
					InputMax = max,

					ResultGold = goldAmount,
					ResultFeeCents = fiatFeeCents,
					ResultNetCents = fiatCents,
				}
			);
		}

		/// <summary>
		/// Estimate exchange operation
		/// </summary>
		public static Task<SellingEstimationResult> EstimateSelling(BigInteger goldAmountWei, BigInteger goldTotalVolumeWei, long pricePerGoldOunceCents, BigInteger mntpBalance) {

			if (pricePerGoldOunceCents <= 0) {
				throw new ArgumentException("Illegal gold price");
			}

			var min = ToWei(0.5M / (pricePerGoldOunceCents / 100M)); // gold amount per 50 cents
			var max = goldTotalVolumeWei;
			if (min > max) {
				min = 0;
				max = 0;
			}

			if (goldAmountWei < min) goldAmountWei = min;
			if (goldAmountWei > max) goldAmountWei = max;

			var fiatGrossCents = (long)decimal.Truncate(
				FromWei(goldAmountWei) * pricePerGoldOunceCents
			);
			var fiatFeeCents = MntpToken.getSellingFee(mntpBalance, fiatGrossCents);
			var fiatNetCents = Math.Max(0L, fiatGrossCents - fiatFeeCents);

			// round back
			goldAmountWei = ToWei((fiatGrossCents / 100M) / (pricePerGoldOunceCents / 100M));

			return Task.FromResult(
				new SellingEstimationResult() {
					InputUsed = goldAmountWei,
					InputMin = min,
					InputMax = max,

					ResultGrossCents = fiatGrossCents,
					ResultFeeCents = fiatFeeCents,
					ResultNetCents = fiatNetCents,
				}
			);
		}

		/// <summary>
		/// Adjust exchange values right before actual processing
		/// </summary>
		private static async Task<AdjustResult> AdjustExchangeGoldRate(IServiceProvider services, bool buying, FiatCurrency currency, long fixedGoldRateCents) {

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
		/// Process request on blockchain and check it
		/// </summary>
		public static async Task<bool> ProcessEthBuyingRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.EthBuyRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.BuyRequest
						where
							r.Type == GoldExchangeRequestType.EthRequest &&
							r.Id == requestId &&
							(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.RefFinancialHistory)
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
							request.RefFinancialHistory.RelEthTransactionId = request.EthTransactionId;

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

								request.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								request.RefFinancialHistory.TimeCompleted = request.TimeCompleted;

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
						.Include(_ => _.RefFinancialHistory)
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
							request.RefFinancialHistory.RelEthTransactionId = request.EthTransactionId;

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

								request.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								request.RefFinancialHistory.TimeCompleted = request.TimeCompleted;

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
						.Include(_ => _.RefFinancialHistory)
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
							request.RefFinancialHistory.RelEthTransactionId = request.EthTransactionId;

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

								request.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								request.RefFinancialHistory.TimeCompleted = request.TimeCompleted;

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
						.Include(_ => _.RefFinancialHistory)
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
							request.RefFinancialHistory.RelEthTransactionId = request.EthTransactionId;

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

								request.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								request.RefFinancialHistory.TimeCompleted = request.TimeCompleted;

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

		/// <summary>
		/// Post blockchain transaction for request
		/// </summary>
		public static async Task<bool> ProcessHwTransferRequest(IServiceProvider services, long requestId) {

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();
			var ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			var ticketDesk = services.GetRequiredService<ITicketDesk>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.HWTransferRequest, requestId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var request = await (
						from r in dbContext.TransferRequest
						where
							r.Id == requestId &&
							(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm)
						select r
					)
						.Include(_ => _.User)
						.Include(_ => _.RefFinancialHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
					;

					if (request == null) {
						return false;
					}

					try {

						// set next check time
						request.TimeNextCheck = DateTime.UtcNow + QueuesUtils.GetNextCheckDelay(request.TimeCreated, TimeSpan.FromSeconds(10), 4);

						// initiate blockchain transaction
						if (request.Status == GoldExchangeRequestStatus.Prepared) {

							// update status to prevent double spending
							request.Status = GoldExchangeRequestStatus.BlockchainInit;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, "Blockchain transaction init");
							}
							catch { }

							var amount = BigInteger.Parse(request.AmountWei);
							var goldBalance = await ethereumReader.GetUserGoldBalance(request.User.UserName);

							// cancelled
							if (amount < 1 || amount > goldBalance) {

								request.Status = GoldExchangeRequestStatus.Cancelled;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, "Request has been cancelled. Invalid amount specified");
								}
								catch { }

								return false;
							}

							// save eth transaction
							request.EthTransactionId = await ethereumWriter.TransferGoldFromHotWallet(
								toAddress: request.DestinationAddress,
								amount: amount,
								userId: request.User.UserName
							);
							request.RefFinancialHistory.RelEthTransactionId = request.EthTransactionId;

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

								request.RefFinancialHistory.Status = success ? FinancialHistoryStatus.Completed : FinancialHistoryStatus.Failed;
								request.RefFinancialHistory.TimeCompleted = request.TimeCompleted;

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
						logger.Error(e, $"Failed to process hw transferring request #{request.Id}");
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

		public enum BuySellPreparationResult {

			Success,
			InvalidArgs,
			NotFound,
			MutexFailure,
		}
	}
}
