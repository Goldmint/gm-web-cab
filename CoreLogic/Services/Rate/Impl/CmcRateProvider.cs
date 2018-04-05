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

	public class CmcRateProvider : ICryptoassetRateProvider {

		// CMC has requirement: max 10 requests/min
		private const long CacheTimeoutSeconds = 20;

		private readonly ILogger _logger;

		#region ETH

		private long _ethUsdStamp;
		private long _ethUsdValue;
		private readonly object _ethUsdMonitor = new object();

		#endregion

		public CmcRateProvider(LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<long> GetRate(CryptoExchangeAsset asset, FiatCurrency currency) {

			long value = 0;

			if (asset == CryptoExchangeAsset.ETH) {
				if (currency == FiatCurrency.USD) {
					value = await EthUsd();
				}
			}

			return value;
		}

		// ---

		#region ETH

		private Task<long> EthUsd() {

			var url = "https://api.coinmarketcap.com/v1/ticker/ethereum/?convert=USD";
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

		#endregion

		// ---

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
				var ex = new Exception($"Failed to find field {field} in response of {url}");
				_logger.Error(ex);
				throw ex;
			}

			if (!decimal.TryParse(resultAsDict[field], NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var dval)) {
				var ex = new Exception($"Failed to parse field {field}=`{resultAsDict[field]}` from response of {url}");
				_logger.Error(ex);
				throw ex;
			}

			return (long)Math.Round(dval * 100);
		}

		internal class CmcResponse {

			public string id { get; set; }
			public string name { get; set; }
			public string symbol { get; set; }
			public string rank { get; set; }
			public string last_updated { get; set; }
			public string price_btc { get; set; }

			public string price_usd { get; set; }
			public string price_eur { get; set; }
		}
	}
}
