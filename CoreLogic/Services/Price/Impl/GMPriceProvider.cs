using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Price.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Price.Impl {

	public sealed class GMPriceProvider : IGoldPriceProvider, IEthPriceProvider {

		private readonly Options _opts;
		private readonly ILogger _logger;

		public GMPriceProvider(Action<Options> opts, ILogger logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_opts = new Options();
			opts?.Invoke(_opts);
		}

		public async Task<Models.CurrencyPrice> RequestGoldPrice(TimeSpan timeout) {
			return await PerformRequest(Common.CurrencyPrice.Gold, _opts.GoldUrl, timeout);
		}

		public async Task<Models.CurrencyPrice> RequestEthPrice(TimeSpan timeout) {
			return await PerformRequest(Common.CurrencyPrice.Eth, _opts.EthUrl, timeout);
		}

		// ---

		private long RoundCents(double value) {
			return (long)Math.Round(value * 100);
		}

		private async Task<Models.CurrencyPrice> PerformRequest(Common.CurrencyPrice type, string url, TimeSpan timeout) {

			SvcResponse result = null;

			using (var req = new Request(_logger)) {
				await req
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							result = await res.ToJson<SvcResponse>();
						}
					})
					.SendGet(url, timeout)
				;
			}

			if (result == null) {
				var ex = new Exception($"Failed to get gold rate");
				_logger.Error(ex, ex.ToString());
				throw ex;
			}

			return new Models.CurrencyPrice(
				cur: type,
				stamp: DateTimeOffset.FromUnixTimeSeconds(result.result.timestamp).UtcDateTime,
				usd: RoundCents(result.result.usd),
				eur: RoundCents(result.result.eur)
			);
		}



		// ---

		public sealed class Options {

			public string GoldUrl { get; set; }
			public string EthUrl { get; set; }
		}

		#pragma warning disable IDE1006
		internal sealed class SvcResponse {

			public string message { get; set; }
			public ResultData result { get; set; }

			public sealed class ResultData {

				public long timestamp { get; set; }
				public double usd { get; set; }
				public double eur { get; set; }
			}
		}
	}
}
