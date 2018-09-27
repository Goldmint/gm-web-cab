using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;

namespace Goldmint.CoreLogic.Finance {

	public static class Estimation {

		public static BigInteger AssetPerGold(EthereumToken ethereumToken, long centsPerAssetRate, long centsPerGoldRate) {

			var decimals = 0;

			if (ethereumToken == EthereumToken.Eth) {
				decimals = TokensPrecision.Ethereum;
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			// round down
			return new BigInteger(centsPerGoldRate) * BigInteger.Pow(10, decimals) / new BigInteger(centsPerAssetRate);
		}

		public static bool IsFixedRateThresholdExceeded(long fixedRateCents, long currentRateCents, double threshold) {
			return (double) Math.Abs(fixedRateCents - currentRateCents) / (double) fixedRateCents > threshold;
		}

		public static BigInteger SellingFeeForCrypto(EthereumToken ethereumToken, BigInteger amount) {
		
			// 0.1% - round down
			if (ethereumToken == EthereumToken.Eth) {
				return amount / new BigInteger(1000);
			}

			return BigInteger.Zero;
		}

		public static long SellingFeeForFiat(long amount, BigInteger mntAmount) {

			// round down

			var mult = BigInteger.Pow(10, TokensPrecision.EthereumMntp);

			if (mntAmount >= 10000 * mult) {
				return (long)(new BigInteger(amount) * 75 / 10000);
			}

			if (mntAmount >= 1000 * mult) {
				return (long)(new BigInteger(amount) * 15 / 1000);
			}

			if (mntAmount >= 10 * mult) {
				return (long)(new BigInteger(amount) * 25 / 1000);
			}

			return (long)(new BigInteger(amount) * 3 / 100);
		}


		#region Buy GOLD

		public static Task<BuyGoldFiatResult> BuyGoldFiat(IServiceProvider services, FiatCurrency fiatCurrency, long fiatAmountCents, long? knownGoldRateCents = null) {

			if (fiatAmountCents <= 0) {
				return Task.FromResult(
					new BuyGoldFiatResult()
				);
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForBuying(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new BuyGoldFiatResult() {
						Status = BuyGoldStatus.TradingDisallowed,
					}
				);
			}

			// round down
			var goldAmount = new BigInteger(fiatAmountCents) * BigInteger.Pow(10, TokensPrecision.EthereumGold) / new BigInteger(goldRate.Value);

			return Task.FromResult(
				new BuyGoldFiatResult() {
					Allowed = true,
					Status = BuyGoldStatus.Success,
					ExchangeCurrency = fiatCurrency,
					CentsPerGoldRate = goldRate.Value,
					ResultCentsAmount = fiatAmountCents,
					ResultGoldAmount = goldAmount,
				}
			);
		}

		public static Task<BuyGoldCryptoResult> BuyGoldCrypto(
		    IServiceProvider services, 
		    EthereumToken ethereumToken, 
		    FiatCurrency fiatCurrency, 
		    BigInteger cryptoAmount, 
		    long? knownGoldRateCents = null, 
		    long? knownCryptoRateCents = null)
		{

			if (cryptoAmount <= 0)
			{
				return Task.FromResult(new BuyGoldCryptoResult());
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;
			var decimals = 0;

			if (ethereumToken == EthereumToken.Eth)
			{
				decimals = TokensPrecision.Ethereum;
				cryptoRate = safeRates.GetRateForSelling(CurrencyRateType.Eth, fiatCurrency);
			}
			else
			{
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}


			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0)
			{
				return Task.FromResult(new BuyGoldCryptoResult()
				{
					Status = BuyGoldStatus.TradingDisallowed,
				});
			}

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForBuying(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				});
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round down
			var goldAmount = cryptoAmount * BigInteger.Pow(10, decimals) / assetPerGold;

			return Task.FromResult(new BuyGoldCryptoResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerAssetRate = cryptoRate.Value,
				CentsPerGoldRate = goldRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultAssetAmount = cryptoAmount,
				ResultGoldAmount = goldAmount,
			});
		}

		public static Task<BuyGoldFiatResult> BuyGoldFiatRev(IServiceProvider services, FiatCurrency fiatCurrency, BigInteger requiredGoldAmount, long? knownGoldRateCents = null) {

			if (requiredGoldAmount <= 0) {
				return Task.FromResult(
					new BuyGoldFiatResult()
				);
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForBuying(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new BuyGoldFiatResult() {
						Status = BuyGoldStatus.TradingDisallowed,
					}
				);
			}

			// round up
			var exchangeAmount = (requiredGoldAmount * new BigInteger(goldRate.Value) + BigInteger.Pow(10, TokensPrecision.EthereumGold) - 1) / BigInteger.Pow(10, TokensPrecision.EthereumGold);
			if (exchangeAmount > long.MaxValue) {
				return Task.FromResult(
					new BuyGoldFiatResult() {
						Status = BuyGoldStatus.ValueOverflow,
					}
				);
			}

			return Task.FromResult(
				new BuyGoldFiatResult() {
					Allowed = true,
					Status = BuyGoldStatus.Success,
					ExchangeCurrency = fiatCurrency,
					CentsPerGoldRate = goldRate.Value,
					ResultCentsAmount = (long)exchangeAmount,
					ResultGoldAmount = requiredGoldAmount,
				}
			);
		}

		public static Task<BuyGoldCryptoResult> BuyGoldCryptoRev(IServiceProvider services, EthereumToken ethereumToken, FiatCurrency fiatCurrency, BigInteger requiredGoldAmount, long? knownGoldRateCents = null, long? knownCryptoRateCents = null) {

			if (requiredGoldAmount <= 0) {
				return Task.FromResult(new BuyGoldCryptoResult());
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;

			if (ethereumToken == EthereumToken.Eth) {
				cryptoRate = safeRates.GetRateForSelling(CurrencyRateType.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return Task.FromResult(new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				});
			}

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForBuying(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				});
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round up
			var cryptoAmount = (requiredGoldAmount * assetPerGold + BigInteger.Pow(10, TokensPrecision.EthereumGold) - 1) / BigInteger.Pow(10, TokensPrecision.EthereumGold);

			return Task.FromResult(new BuyGoldCryptoResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerAssetRate = cryptoRate.Value,
				CentsPerGoldRate = goldRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultAssetAmount = cryptoAmount,
				ResultGoldAmount = requiredGoldAmount,
			});
		}

		public enum BuyGoldStatus {

			InvalidArgs,
			ValueOverflow,
			TradingDisallowed,
			Success,
		}
		
		public class BuyGoldFiatResult {

			/// <summary>
			/// Overall status
			/// </summary>
			public bool Allowed { get; internal set; } = false;

			/// <summary>
			/// Extended status
			/// </summary>
			public BuyGoldStatus Status { get; internal set; } = BuyGoldStatus.InvalidArgs;

			/// <summary>
			/// Fiat currency in a middle
			/// </summary>
			public FiatCurrency ExchangeCurrency { get; internal set; }

			/// <summary>
			/// Cents per GOLD
			/// </summary>
			public long CentsPerGoldRate { get; internal set; }

			/// <summary>
			/// Resulting cents amount
			/// </summary>
			public long ResultCentsAmount { get; internal set; }

			/// <summary>
			/// Resulting GOLD amount
			/// </summary>
			public BigInteger ResultGoldAmount { get; internal set; }
		}

		public sealed class BuyGoldCryptoResult {

			/// <summary>
			/// Overall status
			/// </summary>
			public bool Allowed { get; internal set; } = false;

			/// <summary>
			/// Extended status
			/// </summary>
			public BuyGoldStatus Status { get; internal set; } = BuyGoldStatus.InvalidArgs;

			/// <summary>
			/// Input cryptoasset type
			/// </summary>
			public EthereumToken Asset { get; internal set; }
			
			/// <summary>
			/// Fiat currency in a middle
			/// </summary>
			public FiatCurrency ExchangeCurrency { get; internal set; }

			/// <summary>
			/// Cents per GOLD
			/// </summary>
			public long CentsPerGoldRate { get; internal set; }

			/// <summary>
			/// Cents per cryptoasset
			/// </summary>
			public long CentsPerAssetRate { get; internal set; }
			
			/// <summary>
			/// Cryptoasset amount per GOLD rate
			/// </summary>
			public BigInteger CryptoPerGoldRate { get; internal set; }

			/// <summary>
			/// Resulting asset amount
			/// </summary>
			public BigInteger ResultAssetAmount { get; internal set; }

			/// <summary>
			/// Resulting GOLD amount
			/// </summary>
			public BigInteger ResultGoldAmount { get; internal set; }
		}

		#endregion


		#region Sell GOLD

		public static Task<SellGoldFiatResult> SellGoldFiat(IServiceProvider services, FiatCurrency fiatCurrency, BigInteger goldAmount, long? knownGoldRateCents = null) {

			if (goldAmount <= 0) {
				return Task.FromResult(
					new SellGoldFiatResult()
				);
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForSelling(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new SellGoldFiatResult() {
						Status = SellGoldStatus.TradingDisallowed,
					}
				);
			}

			var exchangeAmount = goldAmount * new BigInteger(goldRate.Value) / BigInteger.Pow(10, TokensPrecision.EthereumGold);
			if (exchangeAmount > long.MaxValue) {
				return Task.FromResult(
					new SellGoldFiatResult() {
						Status = SellGoldStatus.ValueOverflow,
					}
				);
			}

			return Task.FromResult(
				new SellGoldFiatResult() {
					Allowed = true,
					Status = SellGoldStatus.Success,
					CentsPerGoldRate = goldRate.Value,
					ExchangeCurrency = fiatCurrency,
					ResultGoldAmount = goldAmount,
					ResultCentsAmount = (long)exchangeAmount,
				}
			);
		}

		public static Task<SellGoldCryptoResult> SellGoldCrypto(IServiceProvider services, EthereumToken ethereumToken, FiatCurrency fiatCurrency, BigInteger goldAmount, long? knownGoldRateCents = null, long? knownCryptoRateCents = null) {

			if (goldAmount <= 0) {
				return Task.FromResult(new SellGoldCryptoResult());
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;

			if (ethereumToken == EthereumToken.Eth) {
				cryptoRate = safeRates.GetRateForBuying(CurrencyRateType.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return Task.FromResult(new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				});
			}

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForSelling(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new SellGoldCryptoResult() {
						Status = SellGoldStatus.TradingDisallowed,
					}
				);
			}
			
			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round down
			var cryptoAmount = goldAmount * assetPerGold / BigInteger.Pow(10, TokensPrecision.EthereumGold);

			return Task.FromResult(new SellGoldCryptoResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerGoldRate = goldRate.Value,
				CentsPerAssetRate = cryptoRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultGoldAmount = goldAmount,
				ResultAssetAmount = cryptoAmount,
			});
		}

		public static Task<SellGoldFiatResult> SellGoldFiatRev(IServiceProvider services, FiatCurrency fiatCurrency, long requiredFiatAmountWithFeeCents, long? knownGoldRateCents = null) {

			if (requiredFiatAmountWithFeeCents <= 0) {
				return Task.FromResult(new SellGoldFiatResult());
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForSelling(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(new SellGoldFiatResult() {
					Status = SellGoldStatus.TradingDisallowed,
				});
			}

			// round up
			var goldAmountToSell = (requiredFiatAmountWithFeeCents * BigInteger.Pow(10, TokensPrecision.EthereumGold) + new BigInteger(goldRate.Value - 1)) / new BigInteger(goldRate.Value);

			return Task.FromResult(
				new SellGoldFiatResult() {
					Allowed = true,
					Status = SellGoldStatus.Success,
					ExchangeCurrency = fiatCurrency,
					CentsPerGoldRate = goldRate.Value,
					ResultGoldAmount = goldAmountToSell,
					ResultCentsAmount = requiredFiatAmountWithFeeCents,
				}
			);
		}

		public static Task<SellGoldCryptoResult> SellGoldCryptoRev(IServiceProvider services, EthereumToken ethereumToken, FiatCurrency fiatCurrency, BigInteger requiredCryptoAmountWithFee, long? knownGoldRateCents = null, long? knownCryptoRateCents = null) {

			if (requiredCryptoAmountWithFee <= 0) {
				return Task.FromResult(new SellGoldCryptoResult());
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;
			var decimals = 0;

			if (ethereumToken == EthereumToken.Eth) {
				decimals = TokensPrecision.Ethereum;
				cryptoRate = safeRates.GetRateForBuying(CurrencyRateType.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return Task.FromResult(new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				});
			}

			var goldRate = knownGoldRateCents ?? safeRates.GetRateForSelling(CurrencyRateType.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				});
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round up
			var goldAmount = (requiredCryptoAmountWithFee * BigInteger.Pow(10, decimals) + assetPerGold - 1) / assetPerGold;

			return Task.FromResult(new SellGoldCryptoResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerGoldRate = goldRate.Value,
				CentsPerAssetRate = cryptoRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultGoldAmount = goldAmount,
				ResultAssetAmount = requiredCryptoAmountWithFee,
			});
		}

		public enum SellGoldStatus {

			InvalidArgs,
			ValueOverflow,
			TradingDisallowed,
			Success,
		}

		public class SellGoldFiatResult {

			/// <summary>
			/// Overall status
			/// </summary>
			public bool Allowed { get; internal set; } = false;

			/// <summary>
			/// Extended status
			/// </summary>
			public SellGoldStatus Status { get; internal set; } = SellGoldStatus.InvalidArgs;

			/// <summary>
			/// Fiat currency in a middle
			/// </summary>
			public FiatCurrency ExchangeCurrency { get; internal set; }

			/// <summary>
			/// Cents per GOLD
			/// </summary>
			public long CentsPerGoldRate { get; internal set; }

			/// <summary>
			/// Result gold amount
			/// </summary>
			public BigInteger ResultGoldAmount { get; internal set; }

			/// <summary>
			/// Result cents
			/// </summary>
			public long ResultCentsAmount { get; internal set; }
		}

		public sealed class SellGoldCryptoResult {

			/// <summary>
			/// Overall status
			/// </summary>
			public bool Allowed { get; internal set; } = false;

			/// <summary>
			/// Extended status
			/// </summary>
			public SellGoldStatus Status { get; internal set; } = SellGoldStatus.InvalidArgs;

			/// <summary>
			/// Input cryptoasset type
			/// </summary>
			public EthereumToken Asset { get; internal set; }
			
			/// <summary>
			/// Fiat currency in a middle
			/// </summary>
			public FiatCurrency ExchangeCurrency { get; internal set; }

			/// <summary>
			/// Cents per GOLD
			/// </summary>
			public long CentsPerGoldRate { get; internal set; }

			/// <summary>
			/// Cents per cryptoasset
			/// </summary>
			public long CentsPerAssetRate { get; internal set; }

			/// <summary>
			/// Cryptoasset amount per GOLD rate
			/// </summary>
			public BigInteger CryptoPerGoldRate { get; internal set; }

			/// <summary>
			/// Result gold amount
			/// </summary>
			public BigInteger ResultGoldAmount { get; internal set; }
			
			/// <summary>
			/// Resulting cryptoasset amount
			/// </summary>
			public BigInteger ResultAssetAmount { get; internal set; }
		}

		#endregion
	}
}