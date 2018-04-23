using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class DebugRateProvider : IGoldRateProvider, IEthRateProvider {

		private double _defaultDeltaMult = 0.01d;

		public Task<CurrencyRate> RequestGoldRate(TimeSpan timeout) {
			return Task.FromResult(
				new CurrencyRate(
					cur: CurrencyRateType.Gold,
					stamp: DateTime.UtcNow,
					usd: DefaultValue(133000L)
				)
			);
		}

		public Task<CurrencyRate> RequestEthRate(TimeSpan timeout) {
			return Task.FromResult(
				new CurrencyRate(
					cur: CurrencyRateType.Eth,
					stamp: DateTime.UtcNow,
					usd: DefaultValue(45000L)
				)
			);
		}

		// ---

		private long DefaultValue(long baseCents) {
			var delta = (long)Math.Round(_defaultDeltaMult * baseCents);
			return baseCents + (SecureRandom.GetPositiveInt() % delta * 2) - delta;
		}
	}
}
