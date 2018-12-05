using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl {

	public sealed class EthereumWriter : EthereumBaseClient, IEthereumWriter {

		private readonly RuntimeConfigHolder _runtimeConfig;
		private readonly Nethereum.Web3.Accounts.Account _gmStorageManager;
		private readonly Nethereum.Web3.Accounts.Account _gmMigrationManager;

		public EthereumWriter(AppConfig appConfig, RuntimeConfigHolder runtimeConfig, LogFactory logFactory) : base(appConfig, logFactory) {
			_runtimeConfig = runtimeConfig;

			_gmStorageManager = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.StorageManagerPk);
			_gmMigrationManager = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.MigrationManagerPk);

			// uses semaphore inside:
			_gmStorageManager.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmStorageManager.Address, EthProvider);
			_gmMigrationManager.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmMigrationManager.Address, EthProvider);
		}

		private Task<HexBigInteger> GetWritingGas() {
			var rc = _runtimeConfig.Clone();
			return Task.FromResult(new HexBigInteger(rc.Ethereum.Gas));
		}

		public async Task<string> SendTransaction(Nethereum.Contracts.Contract contract, string functionName, string from, HexBigInteger gas, HexBigInteger value, params object[] functionInput) {
			
			// TODO: name is invalid, gas is invalid
			var function = contract.GetFunction(functionName);

			Logger.Info($"Calling {functionName}() at gas {gas.Value.ToString()}");

			try {
				return await function.SendTransactionAsync(from, gas, value, functionInput);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to call {functionName}() at gas {gas}");
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

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			var contract = web3.Eth.GetContract(
				StorageContractAbi,
				StorageContractAddress
			);

			return await SendTransaction(
				contract, "transferGoldFromHotWallet",
				_gmStorageManager.Address,
				gas,
				new HexBigInteger(0),
				userAddress, amount, userId
			);
		}

		public async Task<string> ProcessRequestEth(BigInteger requestIndex, BigInteger ethPerGold, BigInteger discountPercentage) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}
			if (ethPerGold <= 0) {
				throw new ArgumentException("Invalid rate");
			}

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			var contract = web3.Eth.GetContract(
				StorageContractAbi,
				StorageContractAddress
			);

			return await SendTransaction(
				contract, "processRequest",
				_gmStorageManager.Address,
				gas,
				new HexBigInteger(0),
				requestIndex, ethPerGold, discountPercentage
			);
		}

		public async Task<string> CancelRequest(BigInteger requestIndex) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			var contract = web3.Eth.GetContract(
				StorageContractAbi,
				StorageContractAddress
			);

			return await SendTransaction(
				contract, "cancelRequest",
				_gmStorageManager.Address,
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

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			var contract = web3.Eth.GetContract(
				StorageContractAbi,
				StorageContractAddress
			);

			return await SendTransaction(
				contract, "processBuyRequestFiat",
				_gmStorageManager.Address,
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

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			var contract = web3.Eth.GetContract(
				StorageContractAbi,
				StorageContractAddress
			);

			return await SendTransaction(
				contract, "processSellRequestFiat",
				_gmStorageManager.Address,
				gas,
				new HexBigInteger(0),
				requestIndex, centsPerGold
			);
		}

		public async Task<string> TransferEther(string address, BigInteger amount) {

			if (amount < 1) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (!ValidationRules.BeValidEthereumAddress(address)) {
				throw new ArgumentException("Invalid eth address");
			}

			var web3 = new Web3(_gmStorageManager, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmStorageManager.Address);

			return await web3.TransactionManager.SendTransactionAsync(
				_gmStorageManager.Address,
				address,
				new HexBigInteger(amount)
			);
		}

		public async Task<string> MigrationContractUnholdToken(string address, MigrationRequestAsset asset, BigInteger amount) {

			if (amount < 1) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (!ValidationRules.BeValidEthereumAddress(address)) {
				throw new ArgumentException("Invalid eth address");
			}

			var web3 = new Web3(_gmMigrationManager, EthProvider);
			var gas = await GetWritingGas();

			var contract = web3.Eth.GetContract(
				MigrationContractAbi,
				MigrationContractAddress
			);

			string funcName = null;
			Nethereum.Contracts.Function func = null;

			if (asset == MigrationRequestAsset.Gold) {
				funcName = "unholdGold";
				func = contract.GetFunction(funcName);
			}
			else if (asset == MigrationRequestAsset.Mnt) {
				funcName = "unholdMntp";
				func = contract.GetFunction(funcName);
			}
			else {
				throw new ArgumentException("Invalid asset");
			}

			try {
				Logger.Info($"Calling {funcName}() at gas {gas.Value.ToString()}");
				return await func.SendTransactionAsync(_gmMigrationManager.Address, gas, new HexBigInteger(0), address, amount);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to call {funcName}() at gas {gas}");
			}

			return null;
		}
	}
}
