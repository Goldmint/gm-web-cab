using Goldmint.Common;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public sealed class EthereumWriter : EthereumBaseClient, IEthereumWriter {

		private readonly Nethereum.Web3.Accounts.Account _gmAccount;

		public EthereumWriter(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {

			_gmAccount = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.StorageControllerManagerPk);

			// uses semaphore inside:
			_gmAccount.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmAccount.Address, EthProvider);
		}

		private async Task<HexBigInteger> GetWritingGasPrice() {
			return await GasPrice();
		}

		// ---

		public async Task<string> TransferGoldFromHotWallet(string userId, string toAddress, BigInteger amount) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user id");
			}
			if (amount < 1) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (!ValidationRules.BeValidEthereumAddress(toAddress)) {
				throw new ArgumentException("Invalid eth address");
			}

			var web3 = new Web3(_gmAccount, EthProvider);
			var gas = await GetWritingGasPrice();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("transferGoldFromHotWallet");

			return await func.SendTransactionAsync(
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				toAddress, amount, userId
			);
		}

		public async Task<string> ProcessBuySellRequest(BigInteger requestIndex, BigInteger ethPerGold) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}
			if (ethPerGold <= 0) {
				throw new ArgumentException("Invalid rate");
			}

			var web3 = new Web3(_gmAccount, EthProvider);
			var gas = await GetWritingGasPrice();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("processRequest");

			return await func.SendTransactionAsync(
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				requestIndex, ethPerGold
			);
		}

		public async Task<string> CancelBuySellRequest(BigInteger requestIndex) {

			var web3 = new Web3(_gmAccount, EthProvider);
			var gas = await GetWritingGasPrice();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("cancelRequest");

			return await func.SendTransactionAsync(
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				requestIndex
			);
		}
	}
}
