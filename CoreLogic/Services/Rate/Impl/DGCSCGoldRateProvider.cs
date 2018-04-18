using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class DGCSCGoldRateProvider : IGoldRateProvider {

		private readonly Options _opts;
		private readonly ILogger _logger;

		public DGCSCGoldRateProvider(LogFactory logFactory, Action<Options> opts) {
			_logger = logFactory.GetLoggerFor(this);
			_opts = new Options() {
				GoldRateUrl = "",
			};
			opts?.Invoke(_opts);
		}

		public async Task<CurrencyRate> RequestGoldRate(TimeSpan timeout) {
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

			DgcscResponse result = null;

			using (var req = new Request(_logger)) {
				await req
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							result = await res.ToJson<DgcscResponse>();
						}
					})
					.SendGet(_opts.GoldRateUrl, timeout)
				;
			}

			if (result == null) {
				var ex = new Exception($"Failed to get gold rate");
				_logger.Error(ex);
				throw ex;
			}

			return new CurrencyRate(
				cur: CurrencyRateType.Gold,
				stamp: DateTime.UtcNow,
				usd: ParseCents(result.GoldPrice.USD.bid)
			);
		}

		// ---

		public sealed class Options {

			public string GoldRateUrl { get; set; }
		}

		internal sealed class DgcscResponse {

			public GoldPriceData GoldPrice { get; set; }

			public sealed class GoldPriceData {

				public Currency USD { get; set; }

				public class Currency {

					public string bid { get; set; }
				}
			}
		}
	}
}
