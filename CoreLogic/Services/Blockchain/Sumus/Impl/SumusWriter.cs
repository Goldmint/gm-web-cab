using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using NLog;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Impl {

	public sealed class SumusWriter : ISumusWriter {

		private readonly AppConfig _appConfig;
		private readonly LogFactory _logFactory;
		private readonly ILogger _logger;

		public SumusWriter(AppConfig appConfig, LogFactory logFactory) {
			_appConfig = appConfig;
			_logger = logFactory.GetLoggerFor(this);
			_logFactory = logFactory;
		}

		public async Task<SentTransaction> TransferToken(Common.Sumus.Signer signer, ulong nonce, byte[] addr, SumusToken asset, BigInteger amount) {
			var tx = Common.Sumus.Transaction.TransferAsset(
				signer,
				nonce,
				addr,
				asset,
				amount
			);

			var url = string.Format("{0}/tx", _appConfig.Services.Sumus.SumusNodeProxyUrl);
			var body = new ProxyAddTransactionRequest() {
				name = tx.Name,
				data = tx.Data,
			};
			var res = await SumusNodeProxy.Post<ProxyAddTransactionResult, ProxyAddTransactionRequest>(url, body, _logger);
			if (res != null) {
				return new SentTransaction() {
					Nonce = nonce,
					Digest = tx.Digest,
					Hash = tx.Hash,
				};
			}

			return null;
		}

		// ---

		internal class ProxyAddTransactionRequest {
			public string name { get; set; }
			public string data { get; set; }
		}
		internal class ProxyAddTransactionResult { }
	}
}
