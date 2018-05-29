using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
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
		/// Process ETH to GOLD buying request (core-worker)
		/// ETH received at contract, GOLD will be issued
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> OnEthereumContractBuyEvent(IServiceProvider services, BigInteger requestIndex, long internalRequestId, string address, BigInteger amountEth, string txId, int txConfirmationsRequired) {

			if (internalRequestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellRequestProcessingResult.InvalidArgs;
			if (amountEth < 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(txId)) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var runtimeConfig = services.GetRequiredService<RuntimeConfigHolder>().Clone();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();
			var safeRates = services.GetRequiredService<IAggregatedSafeRatesSource>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();

			var query =
				from r in dbContext.BuyGoldRequest
				where
					r.Input == BuyGoldRequestInput.ContractEthPayment &&
					r.Id == internalRequestId &&
					r.Status == BuyGoldRequestStatus.Confirmed &&
					r.Output == BuyGoldRequestOutput.EthereumAddress &&
					r.EthAddress == address
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
					var request = await (query)
						.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
						.Include(_ => _.User)
						.FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of { TextFormatter.FormatTokenAmount(amountEth, Common.Tokens.ETH.Decimals) } ETH is {txId}");
					}
					catch { }

					try {

						// get tx info
						var txInfo = await ethereumReader.CheckTransaction(txId, txConfirmationsRequired);
						if (txInfo.Status != EthTransactionStatus.Success || txInfo.Time == null) {
							return BuySellRequestProcessingResult.InvalidArgs;
						}

						var timeNow = DateTime.UtcNow;

						// ok
						if (request.TimeExpires > txInfo.Time.Value) {

							var ethPerGoldFixedRate = Estimation.AssetPerGold(CryptoCurrency.Eth, request.InputRateCents, request.GoldRateCents);
							var ethActualRate = safeRates.GetRate(CurrencyRateType.Eth);
							var goldActualRate = safeRates.GetRate(CurrencyRateType.Gold);

							var cancelRequest =
								ethPerGoldFixedRate <= 0 || !ethActualRate.CanSell || !goldActualRate.CanBuy ||
								Estimation.IsFixedRateThresholdExceeded(request.InputRateCents, ethActualRate.Usd, runtimeConfig.Gold.SafeRate.Eth.SellEthGoldChangeThreshold) ||
								Estimation.IsFixedRateThresholdExceeded(request.GoldRateCents, goldActualRate.Usd, runtimeConfig.Gold.SafeRate.Gold.BuyEthGoldChangeThreshold)
							;

							if (cancelRequest) {
								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request cancelled internally due to significant currencies rate change");
								}
								catch { }
							}

							// estimated gold amount
							var estimatedGoldAmount = await Estimation.BuyGoldCrypto(
								services: services,
								cryptoCurrency: CryptoCurrency.Eth,
								fiatCurrency: request.ExchangeCurrency,
								cryptoAmount: amountEth,
								knownGoldRateCents: request.GoldRateCents,
								knownCryptoRateCents: request.InputRateCents
							);

							// eth operation
							var ethOp = new DAL.Models.EthereumOperation() {
								Type = cancelRequest? EthereumOperationType.ContractCancelBuyRequest: EthereumOperationType.ContractProcessBuyRequestEth,
								Status = EthereumOperationStatus.Initial,
								RelatedExchangeRequestId = request.Id,

								DestinationAddress = request.EthAddress,
								Rate = ethPerGoldFixedRate.ToString(),
								GoldAmount = estimatedGoldAmount.ResultGoldAmount.ToString(),
								EthRequestIndex = requestIndex.ToString(),
								OplogId = request.OplogId,
								TimeCreated = timeNow,
								TimeNextCheck = timeNow,

								UserId = request.UserId,
								RelUserFinHistoryId = request.RelUserFinHistoryId,
							};
							dbContext.EthereumOperation.Add(ethOp);
							await dbContext.SaveChangesAsync();

							// done
							ethOp.Status = EthereumOperationStatus.Prepared;
							request.Status = cancelRequest? BuyGoldRequestStatus.Cancelled: BuyGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = cancelRequest? UserFinHistoryStatus.Failed: UserFinHistoryStatus.Completed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							request.RelUserFinHistory.SourceAmount = TextFormatter.FormatTokenAmountFixed(amountEth, Tokens.ETH.Decimals);
							request.RelUserFinHistory.DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimatedGoldAmount.ResultGoldAmount, Tokens.GOLD.Decimals);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Ethereum operation #{ ethOp.Id } enqueued");
							}
							catch {
							}

							return cancelRequest? BuySellRequestProcessingResult.Cancelled: BuySellRequestProcessingResult.Success;
						}

						// expired
						else {

							request.Status = BuyGoldRequestStatus.Expired;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = UserFinHistoryStatus.Failed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Failed, $"Request #{request.Id} is expired");
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
		/// Process contract GOLD selling request (core-worker harvester)
		/// GOLD burnt at contract, ETH/fiat will be sent
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> OnEthereumContractSellEvent(IServiceProvider services, BigInteger requestIndex, long internalRequestId, string address, BigInteger amountGold, string txId, int txConfirmationsRequired) {

			if (internalRequestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellRequestProcessingResult.InvalidArgs;
			if (amountGold < 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(txId)) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var runtimeConfig = services.GetRequiredService<RuntimeConfigHolder>().Clone();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();
			var safeRates = services.GetRequiredService<IAggregatedSafeRatesSource>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();

			var query =
				from r in dbContext.SellGoldRequest
				where
					r.Input == SellGoldRequestInput.ContractGoldBurning &&
					r.Id == internalRequestId &&
					r.Status == SellGoldRequestStatus.Confirmed &&
					(
						r.Output == SellGoldRequestOutput.EthAddress ||
						r.Output == SellGoldRequestOutput.CreditCard
					) &&
					r.EthAddress == address
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
					var request = await (query).Include(_ => _.RelUserFinHistory).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of { TextFormatter.FormatTokenAmount(amountGold, Common.Tokens.GOLD.Decimals) } GOLD is {txId}");
					}
					catch { }

					try {

						// get tx info
						var txInfo = await ethereumReader.CheckTransaction(txId, txConfirmationsRequired);
						if (txInfo.Status != EthTransactionStatus.Success || txInfo.Time == null) {
							return BuySellRequestProcessingResult.InvalidArgs;
						}

						var timeNow = DateTime.UtcNow;

						// ETH
						if (request.Output == SellGoldRequestOutput.EthAddress) {

							// ok
							if (request.TimeExpires > txInfo.Time.Value) {

								var ethPerGoldFixedRate = Estimation.AssetPerGold(CryptoCurrency.Eth, request.OutputRateCents, request.GoldRateCents);
								var ethActualRate = safeRates.GetRate(CurrencyRateType.Eth);
								var goldActualRate = safeRates.GetRate(CurrencyRateType.Gold);

								var cancelRequest =
										ethPerGoldFixedRate <= 0 || !ethActualRate.CanBuy || !goldActualRate.CanSell ||
										Estimation.IsFixedRateThresholdExceeded(request.OutputRateCents, ethActualRate.Usd,
											runtimeConfig.Gold.SafeRate.Eth.BuyEthGoldChangeThreshold) ||
										Estimation.IsFixedRateThresholdExceeded(request.GoldRateCents, goldActualRate.Usd,
											runtimeConfig.Gold.SafeRate.Gold.SellEthGoldChangeThreshold)
									;

								if (cancelRequest) {
									try {
										await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending,
											$"Request cancelled internally due to significant currencies rate change");
									}
									catch {
									}
								}

								// estimated crypto amount
								var estimatedCryptoAmount = await Estimation.SellGoldCrypto(
									services: services,
									cryptoCurrency: CryptoCurrency.Eth,
									fiatCurrency: request.ExchangeCurrency,
									goldAmount: amountGold,
									knownGoldRateCents: request.GoldRateCents,
									knownCryptoRateCents: request.OutputRateCents
								);
								var estimatedCryptoAmountFee = Estimation.SellingFeeForCrypto(
									CryptoCurrency.Eth, estimatedCryptoAmount.ResultAssetAmount
								);

								// eth operation
								var ethOp = new DAL.Models.EthereumOperation() {
									Type = cancelRequest
										? EthereumOperationType.ContractCancelSellRequest
										: EthereumOperationType.ContractProcessSellRequestEth,
									Status = EthereumOperationStatus.Initial,
									RelatedExchangeRequestId = request.Id,

									DestinationAddress = request.EthAddress,
									Rate = ethPerGoldFixedRate.ToString(),
									GoldAmount = amountGold.ToString(),
									EthRequestIndex = requestIndex.ToString(),
									OplogId = request.OplogId,
									TimeCreated = timeNow,
									TimeNextCheck = timeNow,

									UserId = request.UserId,
									RelUserFinHistoryId = request.RelUserFinHistoryId,
								};
								dbContext.EthereumOperation.Add(ethOp);
								await dbContext.SaveChangesAsync();

								// done
								ethOp.Status = EthereumOperationStatus.Prepared;
								request.Status = cancelRequest ? SellGoldRequestStatus.Cancelled : SellGoldRequestStatus.Success;
								request.TimeNextCheck = timeNow;
								request.TimeCompleted = timeNow;
								request.RelUserFinHistory.Status = cancelRequest ? UserFinHistoryStatus.Failed : UserFinHistoryStatus.Completed;
								request.RelUserFinHistory.TimeCompleted = timeNow;
								request.RelUserFinHistory.SourceAmount = TextFormatter.FormatTokenAmountFixed(amountGold, Tokens.GOLD.Decimals);
								request.RelUserFinHistory.DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimatedCryptoAmount.ResultAssetAmount - estimatedCryptoAmountFee, Tokens.GOLD.Decimals);
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending,
										$"Request #{request.Id} processed. Ethereum operation #{ethOp.Id} enqueued");
								}
								catch {
								}

								return cancelRequest ? BuySellRequestProcessingResult.Cancelled : BuySellRequestProcessingResult.Success;
							}

							// expired
							else {

								request.Status = SellGoldRequestStatus.Expired;
								request.TimeNextCheck = timeNow;
								request.TimeCompleted = timeNow;
								request.RelUserFinHistory.Status = UserFinHistoryStatus.Failed;
								request.RelUserFinHistory.TimeCompleted = timeNow;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Failed, $"Request #{request.Id} is expired");
								}
								catch {
								}

								return BuySellRequestProcessingResult.Expired;
							}
						}

						// credit card
						if (request.Output == SellGoldRequestOutput.CreditCard) {
							
							// ok
							if (request.TimeExpires > txInfo.Time.Value) {

								// call processing to get cents amount
								var ethOp = new DAL.Models.EthereumOperation() {
									Type = EthereumOperationType.ContractProcessSellRequestFiat,
									Status = EthereumOperationStatus.Initial,
									RelatedExchangeRequestId = request.Id,

									DestinationAddress = request.EthAddress,
									Rate = request.GoldRateCents.ToString(),
									GoldAmount = amountGold.ToString(),
									EthRequestIndex = requestIndex.ToString(),
									OplogId = request.OplogId,
									TimeCreated = timeNow,
									TimeNextCheck = timeNow,

									UserId = request.UserId,
									RelUserFinHistoryId = request.RelUserFinHistoryId,
								};
								dbContext.EthereumOperation.Add(ethOp);
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Ethereum operation #{ ethOp.Id } enqueued");
								}
								catch { }


								// wait for confirmation
								ethOp.Status = EthereumOperationStatus.Prepared;
								request.Status = SellGoldRequestStatus.EthConfirmation;
								request.TimeNextCheck = timeNow;
								request.TimeCompleted = timeNow;
								await dbContext.SaveChangesAsync();

								return BuySellRequestProcessingResult.Success;
							}

							// expired
							else {

								request.Status = SellGoldRequestStatus.Expired;
								request.TimeNextCheck = timeNow;
								request.TimeCompleted = timeNow;
								request.RelUserFinHistory.Status = UserFinHistoryStatus.Failed;
								request.RelUserFinHistory.TimeCompleted = timeNow;
								await dbContext.SaveChangesAsync();

								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Failed, $"Request #{request.Id} is expired");
								}
								catch {
								}

								return BuySellRequestProcessingResult.Expired;
							}
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
		/// Process CreditCard-deposit to GOLD buying request (core-worker)
		/// Fiat received, GOLD will be issued
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> OnCreditCardDepositCompleted(IServiceProvider services, long requestId, long paymentId) {

			if (requestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (paymentId <= 0) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var appConfig = services.GetRequiredService<AppConfig>();
			var runtimeConfig = services.GetRequiredService<RuntimeConfigHolder>().Clone();
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();
			var safeRates = services.GetRequiredService<IAggregatedSafeRatesSource>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();

			var query =
				from r in dbContext.BuyGoldRequest
				where
					r.Input == BuyGoldRequestInput.CreditCardDeposit &&
					r.Id == requestId &&
					r.Status == BuyGoldRequestStatus.Confirmed &&
					r.Output == BuyGoldRequestOutput.EthereumAddress
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellRequestProcessingResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.GoldBuyingReq, requestId)
			;

			return await mutexBuilder.CriticalSection<BuySellRequestProcessingResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query).Include(_ => _.RelUserFinHistory).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					// get payment
					var payment = await(
							from p in dbContext.CreditCardPayment
							where
								p.Type == CardPaymentType.Deposit &&
								p.Id == paymentId &&
								(p.Status == CardPaymentStatus.Success || p.Status == CardPaymentStatus.Failed)
							select p
						)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;
					if (payment == null) {
						return BuySellRequestProcessingResult.InvalidArgs;
					}

					try {

						if (payment.AmountCents <= 0) return BuySellRequestProcessingResult.InvalidArgs;

						var timeNow = DateTime.UtcNow;

						// ok
						if (payment.Status == CardPaymentStatus.Success) {

							// estimated gold amount
							var estimatedGoldAmount = await Estimation.BuyGoldFiat(
								services: services,
								fiatCurrency: request.ExchangeCurrency,
								fiatAmountCents: payment.AmountCents,
								knownGoldRateCents: request.GoldRateCents
							);

							// eth operation
							var ethOp = new DAL.Models.EthereumOperation() {
								Type = EthereumOperationType.ContractProcessBuyRequestFiat,
								Status = EthereumOperationStatus.Initial,
								RelatedExchangeRequestId = request.Id,
							
								DestinationAddress = request.EthAddress,
								Rate = request.GoldRateCents.ToString(),
								GoldAmount = estimatedGoldAmount.ResultGoldAmount.ToString(),
								CentsAmount = payment.AmountCents,
								EthRequestIndex = "0",
								OplogId = request.OplogId,
								TimeCreated = timeNow,
								TimeNextCheck = timeNow,
							
								UserId = request.UserId,
								RelUserFinHistoryId = request.RelUserFinHistoryId,
							};
							dbContext.EthereumOperation.Add(ethOp);
							await dbContext.SaveChangesAsync();

							// done
							ethOp.Status = EthereumOperationStatus.Prepared;
							request.Status = BuyGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = UserFinHistoryStatus.Completed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							request.RelUserFinHistory.SourceAmount = TextFormatter.FormatAmount(payment.AmountCents);
							request.RelUserFinHistory.DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimatedGoldAmount.ResultGoldAmount, Tokens.GOLD.Decimals);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Ethereum operation #{ethOp.Id} enqueued");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Success;
						}
						// failed
						else {
							request.Status = BuyGoldRequestStatus.Cancelled;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = UserFinHistoryStatus.Failed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							request.RelUserFinHistory.Comment = "Failed to charge";
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Failed to charge deposit for buy request #{request.Id}");
							}
							catch {
							}

							return BuySellRequestProcessingResult.Cancelled;
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
		/// Finalize GOLD selling for fiat request (core-worker)
		/// GOLD received and request is processed, fiat will be withdrawn
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> OnCreditCardWithdrawReady(IServiceProvider services, long requestId, BigInteger ethRequestIndex) {

			if (requestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();
			var ethereumReader = services.GetRequiredService<IEthereumReader>();

			var query =
				from r in dbContext.SellGoldRequest
				where
					r.Input == SellGoldRequestInput.ContractGoldBurning &&
					r.Id == requestId &&
					r.Status == SellGoldRequestStatus.EthConfirmation &&
					r.Output == SellGoldRequestOutput.CreditCard
				select r
			;

			// find first
			if (await (query).AsNoTracking().CountAsync() != 1) {
				return BuySellRequestProcessingResult.NotFound;
			}

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.GoldSellingReq, requestId)
			;

			return await mutexBuilder.CriticalSection<BuySellRequestProcessingResult>(async (ok) => {
				if (ok) {

					// get again
					var request = await (query)
						.Include(_ => _.RelUserFinHistory)
						.FirstOrDefaultAsync()
					;
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {

						// get request status
						var ethRequestInfo = await ethereumReader.GetBuySellRequestBaseInfo(ethRequestIndex);
						if (ethRequestInfo.IsPending) {
							return BuySellRequestProcessingResult.InvalidArgs;
						}

						var timeNow = DateTime.UtcNow;
						var payment = (CreditCardPayment)null;

						if (ethRequestInfo.IsSucceeded && ethRequestInfo.OutputAmount > 0) {

							var card = (UserCreditCard)null;

							// TODO: validate ethRequestInfo.OutputAmount
							
							// get card
							if (request.RelOutputId != null) {
								card = await (
										from c in dbContext.UserCreditCard
										where
											c.Id == request.RelOutputId.Value &&
											c.UserId == request.UserId &&
											c.State == CardState.Verified
										select c
									)
									.AsNoTracking()
									.FirstOrDefaultAsync()
								;
							}
							if (card != null) {
								try {
									// enqueue payment
									payment = await The1StPaymentsProcessing.CreateWithdrawPayment(
										services: services,
										card: card,
										currency: request.ExchangeCurrency,
										amountCents: (long) ethRequestInfo.OutputAmount,
										sellRequestId: request.Id,
										oplogId: request.OplogId
									);
									dbContext.CreditCardPayment.Add(payment);
									await dbContext.SaveChangesAsync();

									payment.Status = CardPaymentStatus.Pending;
								}
								catch (Exception e) {
									logger.Error(e, $"Failed to init withdrawal transaction for request #{request.Id}");
									try {
										await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Failed to init withdrawal transaction for request #{request.Id}");
									}
									catch { }
								}
							}
						}

						// done
						if (payment != null) {
							
							request.Status = SellGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = UserFinHistoryStatus.Completed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							request.RelUserFinHistory.SourceAmount = TextFormatter.FormatTokenAmountFixed(ethRequestInfo.InputAmount, Tokens.GOLD.Decimals);
							request.RelUserFinHistory.DestinationAmount = TextFormatter.FormatAmount(payment.AmountCents);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Withdrawal payment #{ payment.Id } enqueued");
							}
							catch { }

							return BuySellRequestProcessingResult.Success;
						}
						else {
							request.Status = SellGoldRequestStatus.Failed;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RelUserFinHistory.Status = UserFinHistoryStatus.Failed;
							request.RelUserFinHistory.TimeCompleted = timeNow;
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} failed. Eth request failed or invalid cents amount passed");
							}
							catch { }

							return BuySellRequestProcessingResult.Cancelled;
						}
					}
					catch (Exception e) {
						logger.Error(e, $"Failed to process sell request #{request.Id}");
					}
					return BuySellRequestProcessingResult.InvalidArgs;
				}
				return BuySellRequestProcessingResult.MutexFailure;
			});
		}

		// ---

		/// <summary>
		/// Ethereum operaion performed and now is checking
		/// </summary>
		public static async Task OnEthereumOperationConfirmationStarted(IServiceProvider services, DAL.Models.EthereumOperation ethOp) {

			// (!) eth operation is locked by mutex here

			if (ethOp == null) throw new ArgumentException("Eth operation is null");

			// ---

			// has request id
			if (ethOp.RelatedExchangeRequestId != null) {

				var dbContext = services.GetRequiredService<ApplicationDbContext>();
				var appConfig = services.GetRequiredService<AppConfig>();
				var notificationQueue = services.GetRequiredService<INotificationQueue>();
				var templateProvider = services.GetRequiredService<ITemplateProvider>();

				// buying / GOLD issued
				if (ethOp.Type == EthereumOperationType.ContractProcessBuyRequestEth ||
				    ethOp.Type == EthereumOperationType.ContractProcessBuyRequestFiat) {

					// get exchange request
					var request = await (
							from r in dbContext.BuyGoldRequest
							where
								r.Id == ethOp.RelatedExchangeRequestId &&
								r.UserId == ethOp.UserId
							select r
						)
						.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
						.Include(_ => _.User)
						.FirstOrDefaultAsync()
					;

					// notification
					if (request?.RelUserFinHistory?.RelUserActivity != null) {

						var rate = "";
						var srcType = "";
						if (ethOp.Type == EthereumOperationType.ContractProcessBuyRequestEth) {
							var ethPerGoldRate = Estimation.AssetPerGold(CryptoCurrency.Eth, request.InputRateCents, request.GoldRateCents);
							rate = TextFormatter.FormatTokenAmount(ethPerGoldRate, Tokens.ETH.Decimals);
							srcType = "ETH";
						}

						if (ethOp.Type == EthereumOperationType.ContractProcessBuyRequestFiat) {
							rate = TextFormatter.FormatAmount(request.GoldRateCents);
							srcType = request.ExchangeCurrency.ToString().ToUpper();
						}

						await EmailComposer.FromTemplate(await templateProvider.GetEmailTemplate(EmailTemplate.ExchangeGoldIssued, request.RelUserFinHistory.RelUserActivity.Locale))
							.ReplaceBodyTag("REQUEST_ID", request.Id.ToString())
							.ReplaceBodyTag("ETHERSCAN_LINK", appConfig.Services.Ethereum.EtherscanTxView + ethOp.EthTransactionId)
							.ReplaceBodyTag("DETAILS_SOURCE", request.RelUserFinHistory.SourceAmount + " " + srcType)
							.ReplaceBodyTag("DETAILS_RATE", rate + " GOLD/" + srcType)
							.ReplaceBodyTag("DETAILS_ESTIMATED", request.RelUserFinHistory.DestinationAmount + " GOLD")
							.ReplaceBodyTag("DETAILS_ADDRESS", TextFormatter.MaskBlockchainAddress(ethOp.DestinationAddress))
							.Initiator(request.RelUserFinHistory.RelUserActivity)
							.Send(request.User.Email, request.User.UserName, notificationQueue)
						;
					}
				}

				// selling / ETH sent
				if (ethOp.Type == EthereumOperationType.ContractProcessSellRequestEth) {

					// get exchange request
					var request = await (
						from r in dbContext.SellGoldRequest
						where
							r.Id == ethOp.RelatedExchangeRequestId &&
							r.UserId == ethOp.UserId
						select r
					)
						.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
						.Include(_ => _.User)
						.FirstOrDefaultAsync()
					;

					// notification
					if (request?.RelUserFinHistory?.RelUserActivity != null) {

						var ethPerGoldRate = Estimation.AssetPerGold(CryptoCurrency.Eth, request.OutputRateCents, request.GoldRateCents);
						var rate = TextFormatter.FormatTokenAmount(ethPerGoldRate, Tokens.ETH.Decimals) + " GOLD/ETH";

						await EmailComposer.FromTemplate(await templateProvider.GetEmailTemplate(EmailTemplate.ExchangeEthTransferred, request.RelUserFinHistory.RelUserActivity.Locale))
							.ReplaceBodyTag("REQUEST_ID", request.Id.ToString())
							.ReplaceBodyTag("ETHERSCAN_LINK", appConfig.Services.Ethereum.EtherscanTxView + ethOp.EthTransactionId)
							.ReplaceBodyTag("DETAILS_SOURCE", request.RelUserFinHistory.SourceAmount + " GOLD")
							.ReplaceBodyTag("DETAILS_RATE", rate)
							.ReplaceBodyTag("DETAILS_ESTIMATED", request.RelUserFinHistory.DestinationAmount + " ETH")
							.ReplaceBodyTag("DETAILS_ADDRESS", TextFormatter.MaskBlockchainAddress(ethOp.DestinationAddress))
							.Initiator(request.RelUserFinHistory.RelUserActivity)
							.Send(request.User.Email, request.User.UserName, notificationQueue)
						;
					}
				}

				// selling / fiat confirmation
				if (ethOp.Type == EthereumOperationType.ContractProcessSellRequestFiat) {
					// TODO: withdrawal payment will be enqueued soon
				}
			}
		}

		/// <summary>
		/// Ethereum operaion completed succesfully or with an error
		/// </summary>
		public static async Task OnEthereumOperationResult(IServiceProvider services, DAL.Models.EthereumOperation ethOp) {

			// (!) eth operation is locked by mutex here

			if (ethOp == null) throw new ArgumentException("Eth operation is null");

			var logger = services.GetLoggerFor(typeof(GoldToken));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var ticketDesk = services.GetRequiredService<IOplogProvider>();

			// ---

			// success
			if (ethOp.Status == EthereumOperationStatus.Success) {

				// enqueue withdrawal payment
				if (ethOp.Type == EthereumOperationType.ContractProcessSellRequestFiat && ethOp.RelatedExchangeRequestId != null) {
					if (BigInteger.TryParse(ethOp.EthRequestIndex ?? "-1", out var index) && index >= 0) {
						await OnCreditCardWithdrawReady(services, ethOp.RelatedExchangeRequestId.Value, index);
					}
				}
			}
		}

		// ---

		public enum BuySellRequestProcessingResult {
			Cancelled,
			Success,
			Expired,
			InvalidArgs,
			NotFound,
			MutexFailure,
		}
	}
}
