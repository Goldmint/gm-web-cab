using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {
#if DEBUG

	public sealed class DebugRateProvider : IGoldRateProvider, IEthRateProvider {

		private double _defaultSpreadMult = 0.01d;
		private long _defaultGoldRate = 131390L;
		private long _defaultEthRate = 73610L;

		public Task<CurrencyRate> RequestGoldRate(TimeSpan timeout) {
			return Task.FromResult(
				new CurrencyRate(
					cur: CurrencyRateType.Gold,
					stamp: DateTime.UtcNow,
					usd: DefaultValue(_defaultGoldRate)
				)
			);
		}

		public Task<CurrencyRate> RequestEthRate(TimeSpan timeout) {
			return Task.FromResult(
				new CurrencyRate(
					cur: CurrencyRateType.Eth,
					stamp: DateTime.UtcNow,
					usd: DefaultValue(_defaultEthRate)
				)
			);
		}

		// ---

		public void SetGoldRate(long cents) {
			_defaultGoldRate = Math.Max(1, cents);
		}

		public void SetEthRate(long cents) {
			_defaultEthRate = Math.Max(1, cents);
		}

		public void SetSpread(double spreadMult) {
			_defaultSpreadMult = Math.Max(0, spreadMult);
		}

		private long DefaultValue(long baseCents) {
			var delta = (long)Math.Round(_defaultSpreadMult * baseCents);
			if (delta > 0) {
				return Math.Max(1, baseCents + (SecureRandom.GetPositiveInt() % delta * 2) - delta);
			}
			return baseCents;
		}
	}

#endif
}
