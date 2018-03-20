using Goldmint.Common;
using Nethereum.Web3;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public abstract class EthereumBaseClient {

		protected ILogger Logger { get; private set; }

		protected Nethereum.JsonRpc.Client.IClient JsonRpcClient { get; private set; }
		protected Nethereum.JsonRpc.Client.IClient JsonRpcLogsClient { get; private set; }

		protected string FiatContractAddress { get; private set; }
		protected string FiatContractABI { get; private set; }
		protected string GoldTokenContractAddress { get; private set; }
		protected string MntpTokenContractAddress { get; private set; }

		// ---

		public EthereumBaseClient(AppConfig appConfig, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			FiatContractAddress = appConfig.Services.Ethereum.FiatContractAddress;
			FiatContractABI = appConfig.Services.Ethereum.FiatContractAbi;

			JsonRpcClient = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.Provider));
			JsonRpcLogsClient = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.LogsProvider));

			// obtain additional info from contract
			Task.Run(async () => {

				var web3 = new Web3(JsonRpcClient);
				var contract = web3.Eth.GetContract(
					FiatContractABI,
					FiatContractAddress
				);

				var func = contract.GetFunction("goldToken");
				GoldTokenContractAddress = await func.CallAsync<string>();

				func = contract.GetFunction("mntpToken");
				MntpTokenContractAddress = await func.CallAsync<string>();

			}).Wait();
		}

	}
}
