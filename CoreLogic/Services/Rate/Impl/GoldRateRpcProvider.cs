using Goldmint.Common;
using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public class GoldRateRpcProvider : IGoldRateProvider {

		private string _jsonRpcUrl;
		private ILogger _logger;

		public GoldRateRpcProvider(string jsonRpcUrl, LogFactory logFactory) {
			_jsonRpcUrl = jsonRpcUrl;
			_logger = logFactory.GetLoggerFor(this);
		}

		public async Task<long> GetGoldRate(FiatCurrency currency) {

			var result = (long?)null;

			using (var req = new Request(_logger)) {
				await req
					.AcceptJson()
					.BodyJsonRpc($"services.goldrate.{currency.ToString().ToLower()}", null)
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							var rpc = await res.ToJsonRpcResult<long?>();
							if (rpc.Result != null && rpc.Error == null) {
								result = rpc.Result.Value;
							}
						}
					})
					.SendPost(_jsonRpcUrl, TimeSpan.FromSeconds(90))
				;
			}

			if (result == null) {
				var ex = new Exception("Failed to get gold rate");
				_logger.Error(ex);
				throw ex;
			}
			return result.Value;
		}
	}
}
