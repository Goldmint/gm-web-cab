using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Serilog;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl {

	public sealed class EthereumWriter : EthereumBaseClient, IEthereumWriter {

		private readonly RuntimeConfigHolder _runtimeConfig;
		private readonly Nethereum.Web3.Accounts.Account _gmEthSender;

		public EthereumWriter(AppConfig appConfig, RuntimeConfigHolder runtimeConfig, ILogger logFactory) : base(appConfig, logFactory) {
			_runtimeConfig = runtimeConfig;
			_gmEthSender = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.EthSenderPk);

			// uses semaphore inside:
			_gmEthSender.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmEthSender.Address, EthProvider);
		}

		private Task<HexBigInteger> GetWritingGas() {
			var rc = _runtimeConfig.Clone();
			return Task.FromResult(new HexBigInteger(rc.Ethereum.Gas));
		}

		public async Task<string> SendTransaction(Nethereum.Contracts.Contract contract, string functionName, string from, HexBigInteger gas, HexBigInteger value, params object[] functionInput) {
			
			// TODO: name is invalid, gas is invalid
			var function = contract.GetFunction(functionName);

			Logger.Information($"Calling {functionName}() at gas {gas.Value.ToString()}");

			try {
				return await function.SendTransactionAsync(from, gas, value, functionInput);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to call {functionName}() at gas {gas}");
			}

			return null;
		}

		public Task<string> GetEthSender() {
			return Task.FromResult(_gmEthSender.Address);
		}

		public async Task<string> SendEth(string address, BigInteger amount) {

			if (amount < 1) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (!ValidationRules.BeValidEthereumAddress(address)) {
				throw new ArgumentException("Invalid eth address");
			}

			var web3 = new Web3(_gmEthSender, EthProvider);
			var gas = await GetWritingGas();
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmEthSender.Address);

			return await web3.TransactionManager.SendTransactionAsync(
				_gmEthSender.Address,
				address,
				new HexBigInteger(amount)
			);
		}
	}
}
