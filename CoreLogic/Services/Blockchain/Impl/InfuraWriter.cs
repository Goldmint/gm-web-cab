using Goldmint.Common;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public sealed class InfuraWriter : InfuraBaseClient, IEthereumWriter {

		private readonly Nethereum.Web3.Accounts.Account _gmAccount;
		private readonly HexBigInteger _defaultGas;

		public InfuraWriter(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {

			_gmAccount = new Nethereum.Web3.Accounts.Account(appConfig.Services.Infura.GMAccountPrivateKey);
			
			// uses semaphore inside:
			_gmAccount.NonceService = new Nethereum.RPC.NonceServices.InMemoryNonceService(_gmAccount.Address, JsonRpcClient);

			_defaultGas = new HexBigInteger(new BigInteger(appConfig.Services.Infura.DefaultGasPriceWei));
		}

		// ---

		public async Task<string> ChangeUserFiatBalance(long userId, FiatCurrency currency, long amountCents) {

			if (userId <= 0) {
				throw new ArgumentException("Invalid user ID");
			}
			if (amountCents == 0) {
				throw new ArgumentException("Amount is equal to 0");
			}

			if (currency == FiatCurrency.USD) {

				var web3 = new Web3(_gmAccount, JsonRpcClient);
				var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

				var contract = web3.Eth.GetContract(
					"[{\"constant\":false,\"inputs\":[{\"name\":\"_userId\",\"type\":\"string\"},{\"name\":\"_amountCents\",\"type\":\"int256\"}],\"name\":\"addFiatTransaction\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
					FiatContractAddress
				);
				var func = contract.GetFunction("addFiatTransaction");

				return await func.SendTransactionAsync(
					_gmAccount.Address,
					_defaultGas,
					new HexBigInteger(0),
					userId.ToString(), new BigInteger(amountCents)
				);
			}

			throw new NotImplementedException("Currency not implemented");
		}

		public async Task<string> ProcessExchangeRequest(BigInteger requestIndex, FiatCurrency currency, long amountCents, long centsPerGoldToken) {

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
					"[{\"constant\":false,\"inputs\":[{\"name\":\"_index\",\"type\":\"uint256\"},{\"name\":\"_amountCents\",\"type\":\"uint256\"},{\"name\":\"_centsPerGold\",\"type\":\"uint256\"}],\"name\":\"processRequest\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
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

		public async Task<string> CancelExchangeRequest(BigInteger requestIndex) {

			var web3 = new Web3(_gmAccount, JsonRpcClient);
			var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(_gmAccount.Address);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":false,\"inputs\":[{\"name\":\"_index\",\"type\":\"uint256\"}],\"name\":\"cancelRequest\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]",
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
	}
}
