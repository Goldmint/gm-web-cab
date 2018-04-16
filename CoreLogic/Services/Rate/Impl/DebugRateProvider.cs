using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class DebugRateProvider : IGoldRateProvider, ICryptoCurrencyRateProvider {

		private double _defaultDeltaMult = 0.01d;

		public Task<GoldRate> RequestGoldRate(TimeSpan timeout) {
			return Task.FromResult(new GoldRate() {
				Usd = DefaultValue(133000L),
			});
		}

		public Task<CryptoRate> RequestCryptoRate(TimeSpan timeout) {
			var ret = new CryptoRate() {
				EthUsd = DefaultValue(45000L),
			};
			return Task.FromResult(ret);
		}

		// ---

		private long DefaultValue(long baseCents) {
			var delta = (long)Math.Round(_defaultDeltaMult * baseCents);
			return baseCents + (SecureRandom.GetPositiveInt() % delta * 2) - delta;
		}
	}
}
