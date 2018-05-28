using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public sealed class EthereumWriter : EthereumBaseClient, IEthereumWriter {

		private readonly RuntimeConfigHolder _runtimeConfig;
		private readonly Nethereum.Web3.Accounts.Account _gmAccount;

		public EthereumWriter(AppConfig appConfig, RuntimeConfigHolder runtimeConfig, LogFactory logFactory) : base(appConfig, logFactory) {
			_runtimeConfig = runtimeConfig;
			_gmAccount = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.StorageControllerManagerPk);

			// uses semaphore inside:
			_gmAccount.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmAccount.Address, EthProvider);
		}

		private Task<HexBigInteger> GetWritingGas() {
			var rc = _runtimeConfig.Clone();
			return Task.FromResult(new HexBigInteger(rc.Ethereum.Gas));
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

		public async Task<string> TransferGoldFromHotWallet(string userId, string userAddress, BigInteger amount) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user id");
			}
			if (amount < 1) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (!ValidationRules.BeValidEthereumAddress(userAddress)) {
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
				userAddress, amount, userId
			);
		}

		public async Task<string> ProcessRequestEth(BigInteger requestIndex, BigInteger ethPerGold) {

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

		public async Task<string> CancelRequest(BigInteger requestIndex) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}

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

		public async Task<string> ProcessBuyRequestFiat(string userId, BigInteger reference, string userAddress, long amountCents, long centsPerGold) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user id");
			}
			if (reference < 1) {
				throw new ArgumentException("Invalid reference");
			}
			if (!ValidationRules.BeValidEthereumAddress(userAddress)) {
				throw new ArgumentException("Invalid eth address");
			}
			if (amountCents < 1) {
				throw new ArgumentException("Invalid cents");
			}
			if (centsPerGold < 1) {
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
				contract.GetFunction("processBuyRequestFiat"),
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				userId, reference, userAddress, amountCents, centsPerGold
			);
		}

		public async Task<string> ProcessSellRequestFiat(BigInteger requestIndex, long centsPerGold) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}
			if (centsPerGold < 1) {
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
				contract.GetFunction("processSellRequestFiat"),
				_gmAccount.Address,
				gas,
				new HexBigInteger(0),
				requestIndex, centsPerGold
			);
		}
	}
}
