using Goldmint.Common;
using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public class CmcRateProvider : IEthereumRateProvider {

		// CMC has requirement: max 10 requests/min
		private const long CacheTimeoutSeconds = 15;

		private readonly string _tickerUrl;
		private readonly ILogger _logger;

		private long _ethUsdStamp = 0;
		private long _ethUsdValue = 0;
		private readonly object _ethUsdMonitor = new object();

		public CmcRateProvider(string tickerUrl, LogFactory logFactory) {
			_tickerUrl = tickerUrl.TrimEnd('/');
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<long> GetEthereumRate(FiatCurrency currency) {

			long value = 0;

			if (currency == FiatCurrency.USD) {
				value = await GetInUsd();
			}

			return value;
		}

		// ---

		private Task<long> GetInUsd() {

			var url = _tickerUrl + "/ethereum/?convert=USD";
			var field = "price_usd";

			var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			var stamp = Interlocked.Read(ref _ethUsdStamp);
			var value = Interlocked.Read(ref _ethUsdValue);

			if (now - stamp > CacheTimeoutSeconds) {
				if (Monitor.TryEnter(_ethUsdMonitor)) {
					try {
						value = PerformRequest(url, field).Result;
						Interlocked.Exchange(ref _ethUsdStamp, now);
						Interlocked.Exchange(ref _ethUsdValue, value);
					}
					catch (Exception e) {
						_logger.Error(e);
					}
					finally {
						Monitor.Exit(_ethUsdMonitor);
					}
				}
			}

			return Task.FromResult(value);
		}

		private async Task<long> PerformRequest(string url, string field) {

			CmcResponse result = null;

			using (var req = new Request(_logger)) {
				await req
					.AcceptJson()
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							var arr = await res.ToJson<CmcResponse[]>();
							result = arr?.FirstOrDefault();
						}
					})
					.SendGet(url, TimeSpan.FromSeconds(90))
				;
			}

			if (result == null) {
				var ex = new Exception($"Failed to get rate from {url}");
				_logger.Error(ex);
				throw ex;
			}

			var resultAsDict = new Dictionary<string, string>();
			Json.ParseInto(Json.Stringify(result), resultAsDict);

			if (!resultAsDict.ContainsKey(field)) {
				var ex = new Exception($"Failed to parse field {field} from response of {url}");
				_logger.Error(ex);
				throw ex;
			}

			return (long)Math.Round(decimal.Parse(resultAsDict[field]) * 100);
		}

		// ---

		internal class CmcResponse {

			public string id { get; set; }
			public string name { get; set; }
			public string symbol { get; set; }
			public string rank { get; set; }
			public string last_updated { get; set; }
			public string price_btc { get; set; }

			public string price_usd { get; set; }
			public string price_eur { get; set; }

			public long ParseCents(string value) {
				return (long)Math.Round(decimal.Parse(value) * 100);
			}
		}
	}
}
