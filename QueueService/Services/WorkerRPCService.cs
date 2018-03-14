using System;
using System.Net;
using System.Numerics;
using AustinHarris.JsonRpc;
using Goldmint.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Goldmint.QueueService.Services {

	public class WorkerRpcService : JsonRpcService {

		private IServiceProvider _services;
		private ILogger _logger;

		public WorkerRpcService(IServiceProvider services, LogFactory logFactory) {
			_services = services;
			_logger = logFactory.GetLoggerFor(this);
		}


		#region Services

		private const string RouteServices = "services.";

		[JsonRpcMethod(RouteServices + "goldrate.usd")]
		public long? ServicesGoldRateUsd() {
			return Workers.GoldRateUpdater.GetGoldRate(FiatCurrency.USD);
		}

		#endregion


		#region Ethereum

		private const string RouteEthereum = "ethereum.";

		[JsonRpcMethod(RouteEthereum + "on_crypto_deposit.eth_usd")]
		public string EthereumOnCryptoDepositEthUsd(string txid, string address, string amount, string user) {

			if (!ValidationRules.BeValidEthereumTransactionId(txid)) {
				JsonRpcContext.SetException(new JsonRpcException((int)HttpStatusCode.BadRequest, "Invalid txid", null));
				return null;
			}

			if (!ValidationRules.BeValidEthereumAddress(address)) {
				JsonRpcContext.SetException(new JsonRpcException((int)HttpStatusCode.BadRequest, "Invalid address", null));
				return null;
			}

			BigInteger bamount;
			if (!BigInteger.TryParse(amount, out bamount) || bamount <= 0L) {
				JsonRpcContext.SetException(new JsonRpcException((int)HttpStatusCode.BadRequest, "Invalid amount", null));
				return null;
			}

			long userId = 0;
			if (!ValidationRules.BeValidUsername(user) || (userId = CoreLogic.User.ExtractId(user)) <= 0) {
				JsonRpcContext.SetException(new JsonRpcException((int)HttpStatusCode.BadRequest, "Invalid user", null));
				return null;
			}

			// own scope
			using (var scopedServices = _services.CreateScope()) {
				JsonRpcContext.SetException(new JsonRpcException((int)HttpStatusCode.InternalServerError, "Method is not implemented", null));
				return null;
			}
		}

		#endregion
	
	}
}
