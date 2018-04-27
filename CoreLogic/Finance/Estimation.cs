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

	public static partial class Estimation {

		public static BigInteger AssetPerGold(CryptoCurrency cryptoCurrency, long centsPerAssetRate, long centsPerGoldRate) {

			var decimals = 0;

			if (cryptoCurrency == CryptoCurrency.Eth) {
				decimals = Common.Tokens.ETH.Decimals;
			}
			else {
				throw new NotImplementedException($"{ cryptoCurrency.ToString() } is not implemented");
			}

			return new BigInteger(centsPerGoldRate) * BigInteger.Pow(10, decimals) / new BigInteger(centsPerAssetRate);
		}

		public static bool IsFixedRateThresholdExceeded(long fixedRateCents, long currentRateCents, double threshold) {
			return (double) Math.Abs(fixedRateCents - currentRateCents) / (double) fixedRateCents > threshold;
		}

		#region Buy GOLD

		public static Task<BuyGoldResult> BuyGold(IServiceProvider services, FiatCurrency exchangeFiatCurrency, long fiatAmountCents) {

			if (fiatAmountCents <= 0) {
				return Task.FromResult(
					new BuyGoldResult()
				);
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var goldRate = safeRates.GetRateForBuying(CurrencyRateType.Gold, exchangeFiatCurrency);

			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new BuyGoldResult() {
						Status = BuyGoldStatus.GoldBuyingNotAllowed,
					}
				);
			}

			var goldAmount = fiatAmountCents * BigInteger.Pow(10, Tokens.GOLD.Decimals) / goldRate.Value / BigInteger.Pow(10, 2);

			return Task.FromResult(
				new BuyGoldResult() {

					Allowed = true,
					Status = BuyGoldStatus.Success,

					ExchangeCurrency = exchangeFiatCurrency,
					CentsPerGoldRate = goldRate.Value,
					TotalGoldAmount = goldAmount,
				}
			);
		}

		public static async Task<BuyGoldWithCryptoResult> BuyGold(IServiceProvider services, CryptoCurrency cryptoCurrency, BigInteger cryptoAmountToSell, FiatCurrency exchangeFiatCurrency) {

			if (cryptoAmountToSell <= 0) {
				return new BuyGoldWithCryptoResult();
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;
			var decimals = 0;

			if (cryptoCurrency == CryptoCurrency.Eth) {
				decimals = Common.Tokens.ETH.Decimals;
				cryptoRate = safeRates.GetRateForSelling(CurrencyRateType.Eth, exchangeFiatCurrency);
			}
			else {
				throw new NotImplementedException($"Estimation (buying) is not implemented for { cryptoCurrency.ToString() }");
			}

			if (cryptoRate == null || cryptoRate <= 0) {
				return new BuyGoldWithCryptoResult() {
					Status = BuyGoldStatus.CryptoSellingNotAllowed,
				};
			}

			var exchangeAmount = cryptoAmountToSell * new BigInteger(cryptoRate.Value) / BigInteger.Pow(10, decimals);
			if (exchangeAmount > long.MaxValue) {
				return new BuyGoldWithCryptoResult() {
					Status = BuyGoldStatus.OutOfLimitCryptoAmount,
				};
			}

			var bgr = await BuyGold(services, exchangeFiatCurrency, (long)exchangeAmount);

			var assetPerGold = BigInteger.Zero;;
			if (bgr.Allowed) {
				assetPerGold = AssetPerGold(cryptoCurrency, cryptoRate.Value, bgr.CentsPerGoldRate);
			}

			return new BuyGoldWithCryptoResult() {

				Allowed = bgr.Allowed,
				Status = bgr.Status,

				ExchangeCurrency = bgr.ExchangeCurrency,
				CentsPerGoldRate = bgr.CentsPerGoldRate,
				TotalGoldAmount = bgr.TotalGoldAmount,

				Asset = cryptoCurrency,
				CentsPerAssetRate = cryptoRate.Value,
				TotalCentsForAsset = (long)exchangeAmount,
				CryptoPerGoldRate = assetPerGold,
			};
		}

		// ---

		public enum BuyGoldStatus {

			InvalidArgs,
			OutOfLimitCryptoAmount,
			GoldBuyingNotAllowed,
			CryptoSellingNotAllowed,
			Success,
		}
		
		public class BuyGoldResult {

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
			/// Resulting GOLD amount
			/// </summary>
			public BigInteger TotalGoldAmount { get; internal set; }
		}

		public sealed class BuyGoldWithCryptoResult : BuyGoldResult {

			/// <summary>
			/// Input cryptoasset type
			/// </summary>
			public CryptoCurrency Asset { get; internal set; }

			/// <summary>
			/// Cents per cryptoasset
			/// </summary>
			public long CentsPerAssetRate { get; internal set; }

			/// <summary>
			/// Converted cryptoasset
			/// </summary>
			public long TotalCentsForAsset { get; internal set; }

			/// <summary>
			/// Cryptoasset amount per GOLD rate
			/// </summary>
			public BigInteger CryptoPerGoldRate { get; internal set; }
		}

		#endregion

		#region Sell GOLD

		public static Task<SellGoldResult> SellGold(IServiceProvider services, BigInteger goldAmountToSell, FiatCurrency exchangeFiatCurrency) {

			if (goldAmountToSell <= 0) {
				return Task.FromResult(
					new SellGoldResult()
				);
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var goldRate = safeRates.GetRateForSelling(CurrencyRateType.Gold, exchangeFiatCurrency);

			if (goldRate == null || goldRate <= 0) {
				return Task.FromResult(
					new SellGoldResult() {
						Status = SellGoldStatus.GoldSellingNotAllowed,
					}
				);
			}

			var exchangeAmount = goldAmountToSell * new BigInteger(goldRate.Value) / BigInteger.Pow(10, Tokens.GOLD.Decimals);
			if (exchangeAmount > long.MaxValue) {
				return Task.FromResult(
					new SellGoldResult() {
						Status = SellGoldStatus.OutOfLimitGoldAmount,
					}
				);
			}

			return Task.FromResult(
				new SellGoldResult() {

					Allowed = true,
					Status = SellGoldStatus.Success,

					ExchangeCurrency = exchangeFiatCurrency,
					CentsPerGoldRate = goldRate.Value,
					TotalCentsForGold = (long)exchangeAmount,
				}
			);
		}

		public static async Task<SellGoldForCryptoResult> SellGold(IServiceProvider services, BigInteger goldAmountToSell, FiatCurrency exchangeFiatCurrency, CryptoCurrency forCryptoCurrency) {

			if (goldAmountToSell <= 0) {
				return new SellGoldForCryptoResult();
			}

			var safeRates = services.GetRequiredService<SafeRatesFiatAdapter>();
			var cryptoRate = (long?)0L;
			var decimals = 0;

			if (forCryptoCurrency == CryptoCurrency.Eth) {
				decimals = Common.Tokens.ETH.Decimals;
				cryptoRate = safeRates.GetRateForBuying(CurrencyRateType.Eth, exchangeFiatCurrency);
			}
			else {
				throw new NotImplementedException($"Estimation (buying) is not implemented for { forCryptoCurrency.ToString() }");
			}

			if (cryptoRate == null || cryptoRate <= 0) {
				return new SellGoldForCryptoResult() {
					Status = SellGoldStatus.CryptoBuyingNotAllowed,
				};
			}

			var cryptoAmount = BigInteger.Zero;
			var assetPerGold = BigInteger.Zero;
			var sgr = await SellGold(services, goldAmountToSell, exchangeFiatCurrency);
			if (sgr.Allowed) {
				cryptoAmount = sgr.TotalCentsForGold * BigInteger.Pow(10, decimals) / cryptoRate.Value;
				assetPerGold = AssetPerGold(forCryptoCurrency, cryptoRate.Value, sgr.CentsPerGoldRate);
			}

			return new SellGoldForCryptoResult() {

				Allowed = sgr.Allowed,
				Status = sgr.Status,

				ExchangeCurrency = sgr.ExchangeCurrency,
				CentsPerGoldRate = sgr.CentsPerGoldRate,
				TotalCentsForGold = sgr.TotalCentsForGold,

				Asset = forCryptoCurrency,
				CentsPerAssetRate = cryptoRate.Value,
				TotalAssetAmount = cryptoAmount,
				CryptoPerGoldRate = assetPerGold,
			};
		}

		// TODO: Fee estimation (MNTP)

		// ---

		public enum SellGoldStatus {

			InvalidArgs,
			OutOfLimitGoldAmount,
			GoldSellingNotAllowed,
			CryptoBuyingNotAllowed,
			Success,
		}

		public class SellGoldResult {

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
			/// Converted GOLD
			/// </summary>
			public long TotalCentsForGold { get; internal set; }
		}

		public sealed class SellGoldForCryptoResult : SellGoldResult {

			/// <summary>
			/// Input cryptoasset type
			/// </summary>
			public CryptoCurrency Asset { get; internal set; }

			/// <summary>
			/// Cents per cryptoasset
			/// </summary>
			public long CentsPerAssetRate { get; internal set; }

			/// <summary>
			/// Resulting cryptoasset amount
			/// </summary>
			public BigInteger TotalAssetAmount { get; internal set; }

			/// <summary>
			/// Cryptoasset amount per GOLD rate
			/// </summary>
			public BigInteger CryptoPerGoldRate { get; internal set; }
		}

		#endregion
	}
}