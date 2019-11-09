using Goldmint.Common;
using Goldmint.CoreLogic.Services.Price;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Goldmint.CoreLogic.Finance {

	public static class Estimation {

		public static BigInteger AssetPerGold(TradableCurrency ethereumToken, long centsPerAssetRate, long centsPerGoldRate) {

			var decimals = 0;

			if (ethereumToken == TradableCurrency.Eth) {
				decimals = TokensPrecision.Ethereum;
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			// round down
			return new BigInteger(centsPerGoldRate) * BigInteger.Pow(10, decimals) / new BigInteger(centsPerAssetRate);
		}

		public static bool IsFixedRateThresholdExceeded(long fixedRateCents, long currentRateCents, double threshold) {
			return (double)Math.Abs(fixedRateCents - currentRateCents) / (double)fixedRateCents > threshold;
		}

		public static BigInteger SellingFeeForCrypto(TradableCurrency ethereumToken, BigInteger amount) {

			// 0.1% - round down
			if (ethereumToken == TradableCurrency.Eth) {
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

		public static BigInteger GetDiscount(double perc, BigInteger value) {
			if (perc <= 0) {
				return BigInteger.Zero;
			}
			if (perc > 99.999) {
				perc = 99.999;
			}
			var p = new BigInteger(perc * 1000);
			return value * p / new BigInteger(100000);
		}

		public static BigInteger GetDiscountRevBody(double perc, BigInteger value) {
			if (perc <= 0) {
				return value;
			}
			if (perc > 99.999) {
				perc = 99.999;
			}
			var p = new BigInteger((100 + perc) * 1000);
			return value * new BigInteger(100) * new BigInteger(1000) / p;
		}

		#region Buy GOLD

		public static async Task<BuyGoldFiatResult> BuyGoldFiat(
			IServiceProvider services, 
			FiatCurrency fiatCurrency, 
			long fiatAmountCents, 
			long? knownGoldPriceCents = null,
			double discount = 0d
		) {

			if (fiatAmountCents <= 0) {
				return new BuyGoldFiatResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();

			var goldPrice = knownGoldPriceCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldPrice == null || goldPrice <= 0) {
				return new BuyGoldFiatResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}
			
			// discount
			var fiatAmountCentsPlusDiscount = new BigInteger(fiatAmountCents) + GetDiscount(discount, new BigInteger(fiatAmountCents));
			if (fiatAmountCentsPlusDiscount > long.MaxValue) {
				return new BuyGoldFiatResult() {
					Status = BuyGoldStatus.ValueOverflow,
				};
			}

			// round down
			var goldAmount = fiatAmountCentsPlusDiscount * BigInteger.Pow(10, TokensPrecision.Sumus) / new BigInteger(goldPrice.Value);

			return new BuyGoldFiatResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				ExchangeCurrency = fiatCurrency,
				Discount = discount,
				CentsPerGoldRate = goldPrice.Value,
				ResultCentsAmount = (long)fiatAmountCentsPlusDiscount,
				ResultGoldAmount = goldAmount,
			};
		}

		public static async Task<BuyGoldCryptoResult> BuyGoldCrypto(
			IServiceProvider services,
			TradableCurrency ethereumToken,
			FiatCurrency fiatCurrency,
			BigInteger cryptoAmount,
			long? knownGoldRateCents = null,
			long? knownCryptoRateCents = null,
			double discount = 0d
		) {

			if (cryptoAmount <= 0) {
				return new BuyGoldCryptoResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();
			var cryptoPrice = (long?)0L;
			var decimals = 0;

			if (ethereumToken == TradableCurrency.Eth) {
				decimals = TokensPrecision.Ethereum;
				cryptoPrice = pricer.GetPriceInFiat(CurrencyPrice.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoPrice = knownCryptoRateCents ?? cryptoPrice;
			if (cryptoPrice == null || cryptoPrice <= 0) {
				return new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoPrice.Value, goldRate.Value);

			// discount
			var cryptoAmountPlusDiscount = cryptoAmount + GetDiscount(discount, cryptoAmount);
			
			// round down
			var goldAmount = cryptoAmountPlusDiscount * BigInteger.Pow(10, decimals) / assetPerGold;

			return new BuyGoldCryptoResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				Discount = discount,
				CentsPerAssetRate = cryptoPrice.Value,
				CentsPerGoldRate = goldRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultAssetAmount = cryptoAmountPlusDiscount,
				ResultGoldAmount = goldAmount,
			};
		}

		public static async Task<BuyGoldFiatResult> BuyGoldFiatRev(
			IServiceProvider services, 
			FiatCurrency fiatCurrency, 
			BigInteger requiredGoldAmount, 
			long? knownGoldRateCents = null,
			double discount = 0d
		) {

			if (requiredGoldAmount <= 0) {
				return new BuyGoldFiatResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new BuyGoldFiatResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}

			// round up
			var exchangeAmount = (requiredGoldAmount * new BigInteger(goldRate.Value) + BigInteger.Pow(10, TokensPrecision.Sumus) - 1) / BigInteger.Pow(10, TokensPrecision.Sumus);
			
			// discount
			var exchangeAmountMinusDiscount = GetDiscountRevBody(discount, exchangeAmount);
			if (exchangeAmountMinusDiscount < 1) {
				exchangeAmountMinusDiscount = 1;
			}
				
			if (exchangeAmountMinusDiscount > long.MaxValue) {
				return new BuyGoldFiatResult() {
					Status = BuyGoldStatus.ValueOverflow,
				};
			}

			return new BuyGoldFiatResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				ExchangeCurrency = fiatCurrency,
				Discount = discount,
				CentsPerGoldRate = goldRate.Value,
				ResultCentsAmount = (long)exchangeAmountMinusDiscount,
				ResultGoldAmount = requiredGoldAmount,
			};
		}

		public static async Task<BuyGoldCryptoResult> BuyGoldCryptoRev(
			IServiceProvider services, 
			TradableCurrency ethereumToken, 
			FiatCurrency fiatCurrency, 
			BigInteger requiredGoldAmount, 
			long? knownGoldRateCents = null, 
			long? knownCryptoRateCents = null,
			double discount = 0d
		) {

			if (requiredGoldAmount <= 0) {
				return new BuyGoldCryptoResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();
			var cryptoRate = (long?)0L;

			if (ethereumToken == TradableCurrency.Eth) {
				cryptoRate = pricer.GetPriceInFiat(CurrencyPrice.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new BuyGoldCryptoResult() {
					Status = BuyGoldStatus.TradingDisallowed,
				};
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round up
			var cryptoAmount = (requiredGoldAmount * assetPerGold + BigInteger.Pow(10, TokensPrecision.Sumus) - 1) / BigInteger.Pow(10, TokensPrecision.Sumus);

			// discount
			var cryptoAmountMinusDiscount = GetDiscountRevBody(discount, cryptoAmount);
			if (cryptoAmountMinusDiscount < 1) {
				cryptoAmountMinusDiscount = 1;
			}

			return new BuyGoldCryptoResult() {
				Allowed = true,
				Status = BuyGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				Discount = discount,
				CentsPerAssetRate = cryptoRate.Value,
				CentsPerGoldRate = goldRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultAssetAmount = cryptoAmountMinusDiscount,
				ResultGoldAmount = requiredGoldAmount,
			};
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
			/// Discount applied
			/// </summary>
			public double Discount { get; internal set; }

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
			public TradableCurrency Asset { get; internal set; }

			/// <summary>
			/// Fiat currency in a middle
			/// </summary>
			public FiatCurrency ExchangeCurrency { get; internal set; }

			/// <summary>
			/// Discount applied
			/// </summary>
			public double Discount { get; internal set; }

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

		public static async Task<SellGoldFiatResult> SellGoldFiat(
			IServiceProvider services, 
			FiatCurrency fiatCurrency, 
			BigInteger goldAmount, 
			long? knownGoldRateCents = null
		) {

			if (goldAmount <= 0) {
				return new SellGoldFiatResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new SellGoldFiatResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			var exchangeAmount = goldAmount * new BigInteger(goldRate.Value) / BigInteger.Pow(10, TokensPrecision.Sumus);
			if (exchangeAmount > long.MaxValue) {
				return new SellGoldFiatResult() {
					Status = SellGoldStatus.ValueOverflow,
				};
			}

			return new SellGoldFiatResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				CentsPerGoldRate = goldRate.Value,
				ExchangeCurrency = fiatCurrency,
				ResultGoldAmount = goldAmount,
				ResultCentsAmount = (long)exchangeAmount,
			};
		}

		public static async Task<SellGoldCryptoResult> SellGoldCrypto(
			IServiceProvider services, 
			TradableCurrency ethereumToken, 
			FiatCurrency fiatCurrency, 
			BigInteger goldAmount, 
			long? knownGoldRateCents = null, 
			long? knownCryptoRateCents = null
		) {

			if (goldAmount <= 0) {
				return new SellGoldCryptoResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();
			var cryptoRate = (long?)0L;

			if (ethereumToken == TradableCurrency.Eth) {
				cryptoRate = pricer.GetPriceInFiat(CurrencyPrice.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round down
			var cryptoAmount = goldAmount * assetPerGold / BigInteger.Pow(10, TokensPrecision.Sumus);

			return new SellGoldCryptoResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerGoldRate = goldRate.Value,
				CentsPerAssetRate = cryptoRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultGoldAmount = goldAmount,
				ResultAssetAmount = cryptoAmount,
			};
		}

		public static async Task<SellGoldFiatResult> SellGoldFiatRev(
			IServiceProvider services, 
			FiatCurrency fiatCurrency, 
			long requiredFiatAmountWithFeeCents, 
			long? knownGoldRateCents = null
		) {

			if (requiredFiatAmountWithFeeCents <= 0) {
				return new SellGoldFiatResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new SellGoldFiatResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			// round up
			var goldAmountToSell = (requiredFiatAmountWithFeeCents * BigInteger.Pow(10, TokensPrecision.Sumus) + new BigInteger(goldRate.Value - 1)) / new BigInteger(goldRate.Value);

			return new SellGoldFiatResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				ExchangeCurrency = fiatCurrency,
				CentsPerGoldRate = goldRate.Value,
				ResultGoldAmount = goldAmountToSell,
				ResultCentsAmount = requiredFiatAmountWithFeeCents,
			};
		}

		public static async Task<SellGoldCryptoResult> SellGoldCryptoRev(
			IServiceProvider services, 
			TradableCurrency ethereumToken, 
			FiatCurrency fiatCurrency, 
			BigInteger requiredCryptoAmountWithFee, 
			long? knownGoldRateCents = null, 
			long? knownCryptoRateCents = null
		) {

			if (requiredCryptoAmountWithFee <= 0) {
				return new SellGoldCryptoResult();
			}

			var pricer = services.GetRequiredService<IPriceSource>();
			var cryptoRate = (long?)0L;
			var decimals = 0;

			if (ethereumToken == TradableCurrency.Eth) {
				decimals = TokensPrecision.Ethereum;
				cryptoRate = pricer.GetPriceInFiat(CurrencyPrice.Eth, fiatCurrency);
			}
			else {
				throw new NotImplementedException($"Not implemented for { ethereumToken.ToString() }");
			}

			cryptoRate = knownCryptoRateCents ?? cryptoRate;
			if (cryptoRate == null || cryptoRate <= 0) {
				return new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			var goldRate = knownGoldRateCents ?? pricer.GetPriceInFiat(CurrencyPrice.Gold, fiatCurrency);
			if (goldRate == null || goldRate <= 0) {
				return new SellGoldCryptoResult() {
					Status = SellGoldStatus.TradingDisallowed,
				};
			}

			var assetPerGold = AssetPerGold(ethereumToken, cryptoRate.Value, goldRate.Value);

			// round up
			var goldAmount = (requiredCryptoAmountWithFee * BigInteger.Pow(10, decimals) + assetPerGold - 1) / assetPerGold;

			return new SellGoldCryptoResult() {
				Allowed = true,
				Status = SellGoldStatus.Success,
				Asset = ethereumToken,
				ExchangeCurrency = fiatCurrency,
				CentsPerGoldRate = goldRate.Value,
				CentsPerAssetRate = cryptoRate.Value,
				CryptoPerGoldRate = assetPerGold,
				ResultGoldAmount = goldAmount,
				ResultAssetAmount = requiredCryptoAmountWithFee,
			};
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
			public TradableCurrency Asset { get; internal set; }

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