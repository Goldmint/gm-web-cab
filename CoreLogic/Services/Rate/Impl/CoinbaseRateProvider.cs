using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class CoinbaseRateProvider : IEthRateProvider {

		private readonly ILogger _logger;

		public CoinbaseRateProvider(LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<CurrencyRate> RequestEthRate(TimeSpan timeout) {
			return await PerformRequest(timeout);
		}

		// ---

		private long ParseCents(string centsStr) {
			if (!decimal.TryParse(centsStr, NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var dval)) {
				var ex = new Exception($"Failed to parse cents from `{centsStr}`");
				_logger.Error(ex);
				throw ex;
			}
			return (long)Math.Round(dval * 100);
		}

		private async Task<CurrencyRate> PerformRequest(TimeSpan timeout) {

			CoinbaseResponse result = null;

			var cur = "ETH";

			using (var req = new Request(_logger)) {
				await req
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							result = await res.ToJson<CoinbaseResponse>();
						}
					})
					.Query("currency="+cur)
					.SendGet("https://api.coinbase.com/v2/exchange-rates", timeout)
				;
			}

			if (result == null) {
				var ex = new Exception($"Failed to get rate of {cur}");
				_logger.Error(ex);
				throw ex;
			}

			return new CurrencyRate(
				cur: CurrencyRateType.Eth,
				stamp: DateTime.UtcNow,
				usd: ParseCents(result.data.rates.USD)
			);
		}

		internal class CoinbaseResponse {

			public Data data { get; set; }

			public class Data {

				public string currency { get; set; }
				public Rates rates { get; set; }

				public class Rates {

					public string USD { get; set; }
				}
			}
		}
	}
}
