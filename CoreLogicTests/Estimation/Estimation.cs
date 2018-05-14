using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Estimation {

	public sealed class Estimation : Test {

		private ServiceProvider _services;
		private SafeRatesDispatcher _ratesDispatcher;
		private DebugRateProvider _ratesProvider;

		public Estimation(ITestOutputHelper testOutput) : base(testOutput) {

			var services = new ServiceCollection();
			SetupServices(services);
			_services = services.BuildServiceProvider();
		}

		private void SetupServices(ServiceCollection services) {
			services.AddSingleton<SafeRatesFiatAdapter>();

			_ratesProvider = new DebugRateProvider();
			services.AddSingleton<IGoldRateProvider>(_ratesProvider);
			services.AddSingleton<IEthRateProvider>(_ratesProvider);

			_ratesDispatcher = new SafeRatesDispatcher(
				null,
				LogFactory,
				opts => {
					opts.PublishPeriod = TimeSpan.FromSeconds(1);
					opts.GoldTtl = TimeSpan.FromSeconds(60);
					opts.EthTtl = TimeSpan.FromSeconds(60);
				}
			);
			_ratesDispatcher.Run();
			services.AddSingleton<IAggregatedSafeRatesSource>(_ratesDispatcher);
		}

		protected override void DisposeManaged() {
			_ratesDispatcher?.Dispose();
			base.DisposeManaged();
		}

		// ---

		[Fact]
		public void AssetPerGold() {
			Assert.True(CoreLogic.Finance.Estimation.AssetPerGold(CryptoCurrency.Eth, 100000, 100000).ToString() == "1000000000000000000");
			Assert.True(CoreLogic.Finance.Estimation.AssetPerGold(CryptoCurrency.Eth, 50000, 100000).ToString() == "2000000000000000000");
			Assert.True(CoreLogic.Finance.Estimation.AssetPerGold(CryptoCurrency.Eth, 300000, 100000).ToString() == "333333333333333333");
			Assert.True(CoreLogic.Finance.Estimation.AssetPerGold(CryptoCurrency.Eth, 30000, 100000).ToString() == "3333333333333333333");
		}

		[Fact]
		public void IsFixedRateThresholdExceeded() {
			Assert.False(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(3333, 3666, 0.1d));
			Assert.False(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(3666, 3333, 0.1d));
			Assert.True(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(3666, 3200, 0.1d));
			Assert.True(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(3200, 3666, 0.1d));
			Assert.False(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(100000, 120000, 0.2d));
			Assert.True(CoreLogic.Finance.Estimation.IsFixedRateThresholdExceeded(100000, 80000, 0.15d));
		}

		[Fact]
		public void BuyGoldSimpleEstimation() {

			var gRate = 140000;
			var eRate = 70000;

			_ratesProvider.SetSpread(0d);
			_ratesProvider.SetGoldRate(gRate);
			_ratesProvider.SetEthRate(eRate);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestGoldRate(TimeSpan.Zero).Result);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestEthRate(TimeSpan.Zero).Result);
			_ratesDispatcher.ForceUpdate().Wait();

			var res = CoreLogic.Finance.Estimation.BuyGold(_services, CryptoCurrency.Eth, 1 * BigInteger.Pow(10, Tokens.ETH.Decimals), FiatCurrency.Usd).Result;
			Assert.True(res.Allowed);
			Assert.True(res.CentsPerGoldRate == gRate);
			Assert.True(res.CentsPerAssetRate == eRate);
			Assert.True(res.CryptoPerGoldRate == 2 * BigInteger.Pow(10, Tokens.ETH.Decimals));
			Assert.True(res.TotalGoldAmount == 5 * BigInteger.Pow(10, Tokens.GOLD.Decimals - 1));

			res = CoreLogic.Finance.Estimation.BuyGold(_services, CryptoCurrency.Eth, 4 * BigInteger.Pow(10, Tokens.ETH.Decimals), FiatCurrency.Usd).Result;
			Assert.True(res.Allowed);
			Assert.True(res.CentsPerGoldRate == gRate);
			Assert.True(res.CentsPerAssetRate == eRate);
			Assert.True(res.CryptoPerGoldRate == 2 * BigInteger.Pow(10, Tokens.ETH.Decimals));
			Assert.True(res.TotalGoldAmount == 2 * BigInteger.Pow(10, Tokens.GOLD.Decimals));
		}

		[Fact]
		public void SellGoldSimpleEstimation() {

			var gRate = 100000;
			var eRate = 200000;

			_ratesProvider.SetSpread(0d);
			_ratesProvider.SetGoldRate(gRate);
			_ratesProvider.SetEthRate(eRate);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestGoldRate(TimeSpan.Zero).Result);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestEthRate(TimeSpan.Zero).Result);
			_ratesDispatcher.ForceUpdate().Wait();

			var res = CoreLogic.Finance.Estimation.SellGold(_services, 1 * BigInteger.Pow(10, Tokens.GOLD.Decimals), FiatCurrency.Usd, CryptoCurrency.Eth).Result;
			Assert.True(res.Allowed);
			Assert.True(res.CentsPerGoldRate == gRate);
			Assert.True(res.CentsPerAssetRate == eRate);
			Assert.True(res.CryptoPerGoldRate == 5 * BigInteger.Pow(10, Tokens.ETH.Decimals - 1));
			Assert.True(res.TotalAssetAmount == 5 * BigInteger.Pow(10, Tokens.ETH.Decimals - 1));
		}

		[Fact]
		public void SellGoldSimpleEstimationReversed1() {

			var gRate = 100000;
			var eRate = 200000;

			_ratesProvider.SetSpread(0d);
			_ratesProvider.SetGoldRate(gRate);
			_ratesProvider.SetEthRate(eRate);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestGoldRate(TimeSpan.Zero).Result);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestEthRate(TimeSpan.Zero).Result);
			_ratesDispatcher.ForceUpdate().Wait();

			var mntAmount = BigInteger.Pow(10, Tokens.MNT.Decimals) * 9;
			var fiatAmount = 100000L;
			var resRev = CoreLogic.Finance.Estimation.SellGoldRev(_services, fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount), FiatCurrency.Usd).Result;
			var resDef = CoreLogic.Finance.Estimation.SellGold(_services, resRev.TotalGoldAmount, FiatCurrency.Usd).Result;
			Assert.True(resRev.Allowed);
			Assert.True(resRev.CentsPerGoldRate == gRate);
			Assert.True(resRev.TotalCentsForGold == resDef.TotalCentsForGold);
			Assert.True(resDef.TotalCentsForGold == fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount));

			mntAmount = BigInteger.Pow(10, Tokens.MNT.Decimals) * 999;
			fiatAmount = 200000L;
			resRev = CoreLogic.Finance.Estimation.SellGoldRev(_services, fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount), FiatCurrency.Usd).Result;
			resDef = CoreLogic.Finance.Estimation.SellGold(_services, resRev.TotalGoldAmount, FiatCurrency.Usd).Result;
			Assert.True(resRev.Allowed);
			Assert.True(resRev.CentsPerGoldRate == gRate);
			Assert.True(resRev.TotalCentsForGold == resDef.TotalCentsForGold);
			Assert.True(resDef.TotalCentsForGold == fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount));

			mntAmount = BigInteger.Pow(10, Tokens.MNT.Decimals) * 9999;
			fiatAmount = 300000L;
			resRev = CoreLogic.Finance.Estimation.SellGoldRev(_services, fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount), FiatCurrency.Usd).Result;
			resDef = CoreLogic.Finance.Estimation.SellGold(_services, resRev.TotalGoldAmount, FiatCurrency.Usd).Result;
			Assert.True(resRev.Allowed);
			Assert.True(resRev.CentsPerGoldRate == gRate);
			Assert.True(resRev.TotalCentsForGold == resDef.TotalCentsForGold);
			Assert.True(resDef.TotalCentsForGold == fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount));

			mntAmount = BigInteger.Pow(10, Tokens.MNT.Decimals) * 10000;
			fiatAmount = 300000L;
			resRev = CoreLogic.Finance.Estimation.SellGoldRev(_services, fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount), FiatCurrency.Usd).Result;
			resDef = CoreLogic.Finance.Estimation.SellGold(_services, resRev.TotalGoldAmount, FiatCurrency.Usd).Result;
			Assert.True(resRev.Allowed);
			Assert.True(resRev.CentsPerGoldRate == gRate);
			Assert.True(resRev.TotalCentsForGold == resDef.TotalCentsForGold);
			Assert.True(resDef.TotalCentsForGold == fiatAmount + CoreLogic.Finance.Estimation.SellingFeeForFiat(fiatAmount, mntAmount));
		}

		[Fact]
		public void SellGoldSimpleEstimationReversed2() {

			var gRate = 100000;
			var eRate = 200000;

			_ratesProvider.SetSpread(0d);
			_ratesProvider.SetGoldRate(gRate);
			_ratesProvider.SetEthRate(eRate);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestGoldRate(TimeSpan.Zero).Result);
			_ratesDispatcher.OnProviderCurrencyRate(_ratesProvider.RequestEthRate(TimeSpan.Zero).Result);
			_ratesDispatcher.ForceUpdate().Wait();

			var cryptoAmount = BigInteger.Pow(10, Tokens.ETH.Decimals);
			var resRev = CoreLogic.Finance.Estimation.SellGoldRev(_services, cryptoAmount + CoreLogic.Finance.Estimation.SellingFeeForCrypto(CryptoCurrency.Eth, cryptoAmount), FiatCurrency.Usd, CryptoCurrency.Eth).Result;
			var resDef = CoreLogic.Finance.Estimation.SellGold(_services, resRev.TotalGoldAmount, FiatCurrency.Usd, CryptoCurrency.Eth).Result;
			Assert.True(resRev.Allowed);
			Assert.True(resRev.CentsPerGoldRate == gRate);
			Assert.True(resRev.TotalCentsForGold == resDef.TotalCentsForGold);
			Assert.True(resDef.TotalAssetAmount == cryptoAmount + CoreLogic.Finance.Estimation.SellingFeeForCrypto(CryptoCurrency.Eth, cryptoAmount));
		}

		// TODO: CoreLogic.Finance.Estimation.* methods - valid/invalid values, valid/invalid rates, allow/disallow trading

		// TODO: fee estimation while selling (mntp balance)
	}
}
