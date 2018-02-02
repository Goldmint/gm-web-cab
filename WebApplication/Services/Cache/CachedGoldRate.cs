using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Services.Cache {

	public class CachedGoldRate {

		private const long CacheTimeoutSeconds = 30;

		private IGoldRateProvider _goldRateProvider;

		private long _usdStamp = 0;
		private long _usdValue = 0;
		private object _usdMonitor = new object();

		// ---

		public CachedGoldRate(IGoldRateProvider goldRateProvider) {
			_goldRateProvider = goldRateProvider;
		}

		public async Task<long> GetGoldRate(FiatCurrency currency) {
			long value = 0;

			if (currency == FiatCurrency.USD) {
				value = await GetUSD();
			}

			return value;
		}

		private Task<long> GetUSD() {
			var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

			var stamp = Interlocked.Read(ref _usdStamp);
			var value = Interlocked.Read(ref _usdValue);

			if (now - stamp > CacheTimeoutSeconds) {
				if (Monitor.TryEnter(_usdMonitor)) {
					try {
						value = _goldRateProvider.GetGoldRate(FiatCurrency.USD).Result; // <-- can't await inside lock
						Interlocked.Exchange(ref _usdStamp, now);
						Interlocked.Exchange(ref _usdValue, value);
					}
					catch { }
					finally {
						Monitor.Exit(_usdMonitor);
					}
				}
			}

			return Task.FromResult(value);
		}
	}
}
