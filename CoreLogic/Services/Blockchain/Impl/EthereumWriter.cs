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
		private BigInteger _writingGas;

		public EthereumWriter(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {

			_writingGas = new BigInteger(appConfig.Services.Ethereum.MinimalGasLimit);
			_gmAccount = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.StorageControllerManagerPk);

			// uses semaphore inside:
			_gmAccount.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmAccount.Address, EthProvider);
		}

		private Task<HexBigInteger> GetWritingGas() {
			return Task.FromResult(new HexBigInteger(_writingGas));
		}

		public async Task<string> SendTransaction(Nethereum.Contracts.Function function, string from, HexBigInteger gas, HexBigInteger value, params object[] functionInput) {
			
			// TODO: name is invalid, gas is invalid
			var fname = function.ToString();

			Logger.Info($"Calling {fname}() at gas {gas.Value.ToString()}");

			try {
				return await function.SendTransactionAsync(from, gas, value, functionInput);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to call {fname}() at gas {gas}");
			}

			return null;
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
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			return await SendTransaction(
				contract.GetFunction("transferGoldFromHotWallet"),
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
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			return await SendTransaction(
				contract.GetFunction("processRequest"),
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				requestIndex, ethPerGold
			);
		}

		public async Task<string> CancelBuySellRequest(BigInteger requestIndex) {

			var web3 = new Web3(_gmAccount, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			return await SendTransaction(
				contract.GetFunction("cancelRequest"),
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				requestIndex
			);
		}
	}
}
