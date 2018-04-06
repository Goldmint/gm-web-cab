using Goldmint.Common;
using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public class CoinbaseRateProvider : ICryptoassetRateProvider {

		// Rate limit is 10k / hour
		private const long CacheTimeoutSeconds = 10;

		private readonly ILogger _logger;

		#region ETH

		private long _ethStamp;
		private readonly object _ethMonitor = new object();

		private long _ethUsdValue;

		#endregion

		public CoinbaseRateProvider(LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<long> GetRate(CryptoCurrency asset, FiatCurrency currency) {

			long value = 0;

			if (asset == CryptoCurrency.ETH) {
				value = await Eth(currency);
			}

			return value;
		}

		// ---

		#region ETH

		private Task<long> Eth(FiatCurrency currency) {

			var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			var stamp = Interlocked.Read(ref _ethStamp);

			var value = 0L;
			switch (currency) {
				case FiatCurrency.USD: value = Interlocked.Read(ref _ethUsdValue); break;
			}

			if (now - stamp > CacheTimeoutSeconds) {
				if (Monitor.TryEnter(_ethMonitor)) {
					try {
						var rates = PerformRequest("ETH").Result;

						Interlocked.Exchange(ref _ethStamp, now);
						Interlocked.Exchange(ref _ethUsdValue, rates.Usd);

						switch (currency) {
							case FiatCurrency.USD: value = rates.Usd; break;
						}
					}
					catch (Exception e) {
						_logger.Error(e);
					}
					finally {
						Monitor.Exit(_ethMonitor);
					}
				}
			}

			return Task.FromResult(value);
		}

		#endregion

		// ---

		private long ParseCents(string centsStr) {
			if (!decimal.TryParse(centsStr, NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var dval)) {
				var ex = new Exception($"Failed to parse cents from `{centsStr}`");
				_logger.Error(ex);
				throw ex;
			}
			return (long)Math.Round(dval * 100);
		}

		private async Task<Rates> PerformRequest(string cur) {

			CoinbaseResponse result = null;

			using (var req = new Request(_logger)) {
				await req
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							result = await res.ToJson<CoinbaseResponse>();
						}
					})
					.Query("currency="+cur)
					.SendGet("https://api.coinbase.com/v2/exchange-rates", TimeSpan.FromSeconds(90))
				;
			}

			if (result == null) {
				var ex = new Exception($"Failed to get rate of {cur}");
				_logger.Error(ex);
				throw ex;
			}

			return new Rates() {
				Currency = result.data.currency,
				Usd = ParseCents(result.data.rates.USD),
			};
		}

		internal class Rates {

			public string Currency { get; set; }

			public long Usd { get; set; }
		}

		internal class CoinbaseResponse {

			public Data data { get; set; }

			public class Data {

				public string currency { get; set; }
				public Rates rates { get; set; }

				public class Rates {

					public string USD { get; set; }
					public string EUR { get; set; }
				}
			}
		}
	}
}
