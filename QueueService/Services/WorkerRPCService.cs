using AustinHarris.JsonRpc;
using Goldmint.Common;

namespace Goldmint.QueueService.Services {

	public class WorkerRPCService : JsonRpcService {

		private const string RouteServices = "services.";

		public WorkerRPCService() { }

		[JsonRpcMethod(RouteServices + "goldrate.usd")]
		public long? GoldRate() {
			return Workers.GoldRateUpdater.GetGoldRate(FiatCurrency.USD);
		}
	}
}
