using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.DAL;
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
		public static async Task<BuySellRequestProcessingResult> ProcessContractBuyRequest(IServiceProvider services, BigInteger requestIndex, long internalRequestId, string address, BigInteger amountEth, string txId, int txConfirmationsRequired) {

			if (internalRequestId <= 0) return BuySellRequestProcessingResult.InvalidArgs;
			if (string.IsNullOrWhiteSpace(address)) return BuySellRequestProcessingResult.InvalidArgs;
			if (amountEth < 0) return BuySellRequestProcessingResult.InvalidArgs;
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
				from r in dbContext.BuyGoldRequest
				where
					r.Input == BuyGoldRequestInput.ContractEthPayment &&
					r.Id == internalRequestId &&
					r.Status == BuyGoldRequestStatus.Confirmed &&
					r.Output == BuyGoldRequestOutput.EthereumAddress &&
					r.InputAddress == address
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
					var request = await (query).Include(_ => _.RefUserFinHistory).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of `{ TextFormatter.FormatTokenAmount(amountEth, Common.Tokens.ETH.Decimals) }` ETH is `{txId}`");
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

							var cancelContract =
								ethPerGoldFixedRate <= 0 || !ethActualRate.CanSell || !goldActualRate.CanBuy ||
								Estimation.IsFixedRateThresholdExceeded(request.InputRateCents, ethActualRate.Usd, runtimeConfig.Gold.SafeRate.Eth.SellChangeThreshold) ||
								Estimation.IsFixedRateThresholdExceeded(request.GoldRateCents, goldActualRate.Usd, runtimeConfig.Gold.SafeRate.Gold.BuyChangeThreshold)
							;

							if (cancelContract) {
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
								Type = cancelContract? EthereumOperationType.ContractCancelBuyRequest: EthereumOperationType.ContractProcessBuyRequest,
								Status = EthereumOperationStatus.Initial,
								RelatedRequestId = request.Id,

								DestinationAddress = request.InputAddress,
								Rate = ethPerGoldFixedRate.ToString(),
								GoldAmount = estimatedGoldAmount.ResultGoldAmount.ToString(),
								EthRequestIndex = requestIndex.ToString(),
								OplogId = request.OplogId,
								TimeCreated = timeNow,
								TimeNextCheck = timeNow,

								UserId = request.UserId,
								RefUserFinHistoryId = request.RefUserFinHistoryId,
							};
							dbContext.EthereumOperation.Add(ethOp);
							await dbContext.SaveChangesAsync();

							// done
							ethOp.Status = EthereumOperationStatus.Prepared;
							request.Status = cancelContract? BuyGoldRequestStatus.Cancelled: BuyGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RefUserFinHistory.SourceAmount = TextFormatter.FormatTokenAmountFixed(amountEth, Tokens.ETH.Decimals);
							request.RefUserFinHistory.DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimatedGoldAmount.ResultGoldAmount, Tokens.GOLD.Decimals);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Ethereum operation #{ ethOp.Id } enqueued");
							}
							catch {
							}

							return cancelContract? BuySellRequestProcessingResult.Cancelled: BuySellRequestProcessingResult.Success;
						}

						// expired
						else {

							request.Status = BuyGoldRequestStatus.Expired;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
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
		/// Process GOLD selling request (core-worker harvester)
		/// </summary>
		public static async Task<BuySellRequestProcessingResult> ProcessContractSellRequest(IServiceProvider services, BigInteger requestIndex, long internalRequestId, string address, BigInteger amountGold, string txId, int txConfirmationsRequired) {

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
					var request = await (query).Include(_ => _.RefUserFinHistory).FirstOrDefaultAsync();
					if (request == null) {
						return BuySellRequestProcessingResult.NotFound;
					}

					try {
						await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"User's Ethereum transaction of `{ TextFormatter.FormatTokenAmount(amountGold, Common.Tokens.GOLD.Decimals) }` GOLD is `{txId}`");
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

							var ethPerGoldFixedRate = Estimation.AssetPerGold(CryptoCurrency.Eth, request.OutputRateCents, request.GoldRateCents);
							var ethActualRate = safeRates.GetRate(CurrencyRateType.Eth);
							var goldActualRate = safeRates.GetRate(CurrencyRateType.Gold);

							var cancelContract =
								ethPerGoldFixedRate <= 0 || !ethActualRate.CanBuy || !goldActualRate.CanSell ||
								Estimation.IsFixedRateThresholdExceeded(request.OutputRateCents, ethActualRate.Usd, runtimeConfig.Gold.SafeRate.Eth.BuyChangeThreshold) ||
								Estimation.IsFixedRateThresholdExceeded(request.GoldRateCents, goldActualRate.Usd, runtimeConfig.Gold.SafeRate.Gold.SellChangeThreshold)
							;

							if (cancelContract) {
								try {
									await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request cancelled internally due to significant currencies rate change");
								}
								catch { }
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
								Type = cancelContract ? EthereumOperationType.ContractCancelSellRequest : EthereumOperationType.ContractProcessSellRequest,
								Status = EthereumOperationStatus.Initial,
								RelatedRequestId = request.Id,

								DestinationAddress = request.OutputAddress,
								Rate = ethPerGoldFixedRate.ToString(),
								GoldAmount = amountGold.ToString(),
								EthRequestIndex = requestIndex.ToString(),
								OplogId = request.OplogId,
								TimeCreated = timeNow,
								TimeNextCheck = timeNow,

								UserId = request.UserId,
								RefUserFinHistoryId = request.RefUserFinHistoryId,
							};
							dbContext.EthereumOperation.Add(ethOp);
							await dbContext.SaveChangesAsync();

							// done
							ethOp.Status = EthereumOperationStatus.Prepared;
							request.Status = cancelContract ? SellGoldRequestStatus.Cancelled : SellGoldRequestStatus.Success;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
							request.RefUserFinHistory.SourceAmount = TextFormatter.FormatTokenAmountFixed(amountGold, Tokens.GOLD.Decimals);
							request.RefUserFinHistory.DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimatedCryptoAmount.ResultAssetAmount - estimatedCryptoAmountFee, Tokens.GOLD.Decimals);
							await dbContext.SaveChangesAsync();

							try {
								await ticketDesk.Update(request.OplogId, UserOpLogStatus.Pending, $"Request #{request.Id} processed. Ethereum operation #{ ethOp.Id } enqueued");
							}
							catch {
							}

							return cancelContract ? BuySellRequestProcessingResult.Cancelled : BuySellRequestProcessingResult.Success;
						}

						// expired
						else {

							request.Status = SellGoldRequestStatus.Expired;
							request.TimeNextCheck = timeNow;
							request.TimeCompleted = timeNow;
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
						logger.Error(e, $"Failed to process sell request #{request.Id}");
					}
				}
				return BuySellRequestProcessingResult.MutexFailure;
			});
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
