using Goldmint.Common;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl {

	public abstract class EthereumBaseClient {

		protected ILogger Logger { get; }

		protected Nethereum.JsonRpc.Client.IClient EthProvider { get; }
		protected Nethereum.JsonRpc.Client.IClient EthLogsProvider { get; }

		protected string StorageContractAddress { get; }
		protected string StorageContractAbi { get; }
		protected string MigrationContractAddress { get; }
		protected string MigrationContractAbi { get; }

		protected string GoldContractAddress { get;}
		protected string GoldContractAbi { get; }
		protected string MntpContractAddress { get; }
		protected string MntpContractAbi { get; }

		// ---

		protected EthereumBaseClient(AppConfig appConfig, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			StorageContractAddress = appConfig.Services.Ethereum.StorageContractAddress;
			StorageContractAbi = appConfig.Services.Ethereum.StorageContractAbi;

			MigrationContractAddress = appConfig.Services.Ethereum.MigrationContractAddress;
			MigrationContractAbi = appConfig.Services.Ethereum.MigrationContractAbi;

			GoldContractAddress = appConfig.Services.Ethereum.GoldContractAddress;
			GoldContractAbi = appConfig.Services.Ethereum.GoldContractAbi;

			MntpContractAddress = appConfig.Services.Ethereum.MntpContractAddress;
			MntpContractAbi = appConfig.Services.Ethereum.MntpContractAbi;

			EthProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.Provider));
			EthLogsProvider = new Nethereum.JsonRpc.Client.RpcClient(new Uri(appConfig.Services.Ethereum.LogsProvider));
		}

		protected async Task<HexBigInteger> GasPrice() {
			var web3 = new Web3(EthProvider);
			return (await web3.Eth.GasPrice.SendRequestAsync());
		}
	}
}
