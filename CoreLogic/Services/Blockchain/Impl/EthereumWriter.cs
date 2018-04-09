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
		private readonly HexBigInteger _defaultGas;

		public EthereumWriter(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {

			_gmAccount = new Nethereum.Web3.Accounts.Account(appConfig.Services.Ethereum.RootAccountPrivateKey);
			
			// uses semaphore inside:
			_gmAccount.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmAccount.Address, JsonRpcClient);

			_defaultGas = new HexBigInteger(new BigInteger(appConfig.Services.Ethereum.DefaultGasPriceWei));
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

			var web3 = new Web3(_gmAccount, JsonRpcClient);
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("transferGoldFromHotWallet");

			return await func.SendTransactionAsync(
				_gmAccount.Address,
				_defaultGas,
				new HexBigInteger(0),
				toAddress, amount, userId
			);
		}

		#region GOLD / Fiat

		public async Task<string> ChangeFiatBalance(string userId, FiatCurrency currency, long amountCents) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user ID");
			}
			if (amountCents == 0) {
				throw new ArgumentException("Amount is equal to 0");
			}

			if (currency == FiatCurrency.USD) {

				var web3 = new Web3(_gmAccount, JsonRpcClient);
				var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

				var contract = web3.Eth.GetContract(
					FiatContractAbi,
					FiatContractAddress
				);
				var func = contract.GetFunction("addFiatTransaction");

				return await func.SendTransactionAsync(
					_gmAccount.Address,
					_defaultGas,
					new HexBigInteger(0),
					userId, new BigInteger(amountCents)
				);
			}

			throw new NotImplementedException("Currency not implemented");
		}

		public async Task<string> PerformGoldFiatExchangeRequest(BigInteger requestIndex, FiatCurrency currency, long amountCents, long centsPerGoldToken) {

			if (requestIndex < 0) {
				throw new ArgumentException("Invalid request index");
			}
			if (amountCents <= 0) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (centsPerGoldToken <= 0) {
				throw new ArgumentException("Invalid gold token price");
			}

			if (currency == FiatCurrency.USD) {

				var web3 = new Web3(_gmAccount, JsonRpcClient);
				var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

				var contract = web3.Eth.GetContract(
					FiatContractAbi,
					FiatContractAddress
				);
				var func = contract.GetFunction("processRequest");

				return await func.SendTransactionAsync(
					_gmAccount.Address,
					_defaultGas,
					new HexBigInteger(0),
					requestIndex, new BigInteger(amountCents), new BigInteger(centsPerGoldToken)
				);
			}

			throw new NotImplementedException("Currency not implemented");
		}

		public async Task<string> CancelGoldFiatExchangeRequest(BigInteger requestIndex) {

			var web3 = new Web3(_gmAccount, JsonRpcClient);
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("cancelRequest");

			return await func.SendTransactionAsync(
				_gmAccount.Address,
				_defaultGas,
				new HexBigInteger(0),
				requestIndex
			);

		}

		public async Task<string> ExchangeGoldFiatOnHotWallet(string userId, bool isBuying, FiatCurrency currency, long amountCents, long centsPerGoldToken) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user id");
			}
			if (amountCents <= 0) {
				throw new ArgumentException("Amount is equal to 0");
			}
			if (centsPerGoldToken <= 0) {
				throw new ArgumentException("Invalid gold token price");
			}

			if (currency == FiatCurrency.USD) {

				var web3 = new Web3(_gmAccount, JsonRpcClient);
				var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

				var contract = web3.Eth.GetContract(
					FiatContractAbi,
					FiatContractAddress
				);
				var func = contract.GetFunction("processInternalRequest");

				return await func.SendTransactionAsync(
					_gmAccount.Address,
					_defaultGas,
					new HexBigInteger(0),
					userId, isBuying, new BigInteger(amountCents), new BigInteger(centsPerGoldToken)
				);
			}

			throw new NotImplementedException("Currency not implemented");
		}

		#endregion

	}
}
