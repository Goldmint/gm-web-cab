using Goldmint.Common;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public abstract class EthereumBaseClient {

		protected ILogger Logger { get; }

		protected Nethereum.JsonRpc.Client.IClient EthProvider { get; }
		protected Nethereum.JsonRpc.Client.IClient EthLogsProvider { get; }

		protected string FiatContractAddress { get; }
		protected string FiatContractAbi { get; }
		protected string GoldTokenContractAddress { get; private set; }
		protected string MntpTokenContractAddress { get; private set; }

		// ---

		protected EthereumBaseClient(AppConfig appConfig, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			FiatContractAddress = appConfig.Services.Ethereum.StorageControllerContractAddress;
			FiatContractAbi = appConfig.Services.Ethereum.StorageControllerContractAbi;

			EthProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.Provider));
			EthLogsProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.LogsProvider));

			// obtain additional info from contract
			Task.Run(async () => {

				var web3 = new Web3(EthProvider);
				var contract = web3.Eth.GetContract(
					FiatContractAbi,
					FiatContractAddress
				);

				var func = contract.GetFunction("goldToken");
				GoldTokenContractAddress = await func.CallAsync<string>();
				Logger.Info("GOLD token address is " + GoldTokenContractAddress);

				func = contract.GetFunction("mntpToken");
				MntpTokenContractAddress = await func.CallAsync<string>();
				Logger.Info("MNTP token address is " + GoldTokenContractAddress);
			}).Wait();
		}

		protected async Task<HexBigInteger> GasPrice() {
			var web3 = new Web3(EthProvider);
			return (await web3.Eth.GasPrice.SendRequestAsync());
		}
	}
}
