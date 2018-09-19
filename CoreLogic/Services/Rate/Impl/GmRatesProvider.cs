using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class GmRatesProvider : IGoldRateProvider, IEthRateProvider
	{

		private readonly Options _opts;
		private readonly ILogger _logger;

		public GmRatesProvider(Action<Options> opts, LogFactory logFactory)
		{
			_logger = logFactory.GetLoggerFor(this);
			_opts = new Options()
			{
				GoldUrl = "",
				EthUrl = "",
			};
			opts?.Invoke(_opts);
		}

		public async Task<CurrencyRate> RequestGoldRate(TimeSpan timeout) {
			return await PerformRequest(CurrencyRateType.Gold, _opts.GoldUrl, timeout);
		}

		public async Task<CurrencyRate> RequestEthRate(TimeSpan timeout) {
			return await PerformRequest(CurrencyRateType.Eth, _opts.EthUrl, timeout);
		}

		// ---

		private long RoundCents(double value) {
			return (long)Math.Round(value * 100);
		}

		private async Task<CurrencyRate> PerformRequest(CurrencyRateType type, string url, TimeSpan timeout) {

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
				_logger.Error(ex);
				throw ex;
			}

			return new CurrencyRate(
				cur: type,
				stamp: DateTimeOffset.FromUnixTimeSeconds(result.result.timestamp).UtcDateTime,
				usd: RoundCents(result.result.usd)
			);
		}

		

		// ---

		public sealed class Options {

			public string GoldUrl { get; set; }
			public string EthUrl { get; set; }
		}

		internal sealed class SvcResponse {

			public string message { get; set; }
			public ResultData result { get; set; }

			public sealed class ResultData {

				public long timestamp { get; set; }
				public double usd { get; set; }
				// public double eur { get; set; }
			}
		}
	}
}
