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

		#region Buy GOLD

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

			return new BuyGoldWithCryptoResult() {

				Allowed = bgr.Allowed,
				Status = bgr.Status,

				ExchangeCurrency = bgr.ExchangeCurrency,
				GoldRateCents = bgr.GoldRateCents,
				GoldAmount = bgr.GoldAmount,

				CryptoCurrency = cryptoCurrency,
				CryptoAmount = cryptoAmountToSell,
				CryptoRateCents = cryptoRate.Value,
				ExchangeFiatCents = (long)exchangeAmount,
			};
		}

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
					GoldRateCents = goldRate.Value,
					GoldAmount = goldAmount,
				}
			);
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

			public bool Allowed { get; internal set; } = false;
			public BuyGoldStatus Status { get; internal set; } = BuyGoldStatus.InvalidArgs;

			public FiatCurrency ExchangeCurrency { get; internal set; }
			public long GoldRateCents { get; internal set; }
			public BigInteger GoldAmount { get; internal set; }
		}

		public sealed class BuyGoldWithCryptoResult : BuyGoldResult {

			public CryptoCurrency CryptoCurrency { get; internal set; }
			public BigInteger CryptoAmount { get; internal set; }
			public long CryptoRateCents { get; internal set; }
			public long ExchangeFiatCents { get; internal set; }
		}

		#endregion

		#region Sell GOLD

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
			var sgr = await SellGold(services, goldAmountToSell, exchangeFiatCurrency);
			if (sgr.Allowed) {
				cryptoAmount = sgr.ExchangeAmountCents * BigInteger.Pow(10, decimals) / cryptoRate.Value;
			}

			return new SellGoldForCryptoResult() {

				Allowed = sgr.Allowed,
				Status = sgr.Status,

				ExchangeCurrency = sgr.ExchangeCurrency,
				GoldRateCents = sgr.GoldRateCents,
				ExchangeAmountCents = sgr.ExchangeAmountCents,

				CryptoCurrency = forCryptoCurrency,
				CryptoRateCents = cryptoRate.Value,
				CryptoAmount = cryptoAmount,
			};
		}

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
					GoldRateCents = goldRate.Value,
					ExchangeAmountCents = (long)exchangeAmount,
				}
			);
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

			public bool Allowed { get; internal set; } = false;
			public SellGoldStatus Status { get; internal set; } = SellGoldStatus.InvalidArgs;

			public FiatCurrency ExchangeCurrency { get; internal set; }
			public long GoldRateCents { get; internal set; }
			public long ExchangeAmountCents { get; internal set; }
		}

		public sealed class SellGoldForCryptoResult : SellGoldResult {

			public CryptoCurrency CryptoCurrency { get; internal set; }
			public long CryptoRateCents { get; internal set; }
			public BigInteger CryptoAmount { get; internal set; }
		}

		#endregion
	}
}