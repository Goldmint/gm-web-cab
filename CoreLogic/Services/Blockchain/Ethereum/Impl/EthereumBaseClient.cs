using Goldmint.Common;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl {

	public abstract class EthereumBaseClient {

		protected ILogger Logger { get; }

		protected Nethereum.JsonRpc.Client.IClient EthProvider { get; }
		protected Nethereum.JsonRpc.Client.IClient EthLogsProvider { get; }

		protected string MntpContractAddress { get; }
		protected string MntpContractAbi { get; }

		protected string PoolContractAddress { get; }
		protected string PoolContractAbi { get; }
		protected string PoolFreezerContractAddress { get; }
		protected string PoolFreezerContractAbi { get; }

		protected EthereumBaseClient(AppConfig appConfig, ILogger logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			MntpContractAddress = appConfig.Services.Ethereum.MntpContractAddress;
			MntpContractAbi = appConfig.Services.Ethereum.MntpContractAbi;

			PoolContractAddress = appConfig.Services.Ethereum.PoolContractAddress;
			PoolContractAbi = appConfig.Services.Ethereum.PoolContractAbi;

			PoolFreezerContractAddress = appConfig.Services.Ethereum.PoolFreezerContractAddress;
			PoolFreezerContractAbi = appConfig.Services.Ethereum.PoolFreezerContractAbi;

			EthProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.Provider));
			EthLogsProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.Provider));
		}

		protected async Task<HexBigInteger> GasPrice() {
			var web3 = new Web3(EthProvider);
			return (await web3.Eth.GasPrice.SendRequestAsync());
		}
	}
}
