using Goldmint.Common;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Estimation {

	public sealed class Estimation : Test {

		public Estimation(ITestOutputHelper testOutput) : base(testOutput) {
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

		// TODO: CoreLogic.Finance.Estimation.* methods - valid/invalid values, valid/invalid rates, allow/disallow trading

		// TODO: fee estimation while selling (mntp balance)
	}
}
