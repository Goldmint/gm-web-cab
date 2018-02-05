using Goldmint.Common;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public class InfuraReader : InfuraBaseClient, IEthereumReader {

		public InfuraReader(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {
		}

		public async Task<BlockchainTransactionStatus> CheckTransaction(string transactionId) {

			if (string.IsNullOrWhiteSpace(transactionId)) {
				throw new ArgumentException("Invalid transaction format");
			}

			var web3 = new Web3(JsonRpcClient);
			var txinfo = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionId);
			
			if (txinfo != null) {
				if (txinfo.BlockNumber.HexValue != null) {
					return BlockchainTransactionStatus.Success;
				}
				return BlockchainTransactionStatus.Pending;
			}

			return BlockchainTransactionStatus.NotFound;
		}

		public async Task<long> GetUserFiatBalance(string userId, FiatCurrency currency) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user ID");
			}

			if (currency == FiatCurrency.USD) {
				var web3 = new Web3(JsonRpcClient);
				var contract = web3.Eth.GetContract(
					"[{\"constant\":true,\"inputs\":[{\"name\":\"_userId\",\"type\":\"string\"}],\"name\":\"getUserFiatBalance\",\"outputs\":[{\"name\":\"\",\"type\":\"int256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
					FiatContractAddress
				);

				var func = contract.GetFunction("getUserFiatBalance");
				var funcRet = await func.CallAsync<BigInteger>(userId);

				return funcRet > long.MaxValue? long.MaxValue: (long)funcRet;
			}

			throw new NotImplementedException("Currency not implemented");
		}

		public async Task<BigInteger> GetUserGoldBalance(string address) {

			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}

			var web3 = new Web3(JsonRpcClient);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
				GoldTokenContractAddress
			);
			var func = contract.GetFunction("balanceOf");
			var funcRet = await func.CallAsync<BigInteger>(address);

			return funcRet;
		}

		public async Task<BigInteger> GetUserMntpBalance(string address) {

			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}

			var web3 = new Web3(JsonRpcClient);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
				MntpTokenContractAddress
			);
			var func = contract.GetFunction("balanceOf");
			var funcRet = await func.CallAsync<BigInteger>(address);

			return funcRet;
		}

		public async Task<BigInteger> GetExchangeRequestsCount() {

			var web3 = new Web3(JsonRpcClient);

			var contract = web3.Eth.GetContract(
					"[{\"constant\":true,\"inputs\":[],\"name\":\"getRequestsCount\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
					FiatContractAddress
				);
			var func = contract.GetFunction("getRequestsCount");
			return await func.CallAsync<BigInteger>();
		}

		public async Task<ExchangeRequestData> GetExchangeRequestByIndex(BigInteger requestIndex) {

			var web3 = new Web3(JsonRpcClient);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":true,\"inputs\":[{\"name\":\"_index\",\"type\":\"uint256\"}],\"name\":\"getRequest\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"},{\"name\":\"\",\"type\":\"string\"},{\"name\":\"\",\"type\":\"string\"},{\"name\":\"\",\"type\":\"bool\"},{\"name\":\"\",\"type\":\"uint8\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
				FiatContractAddress
			);
			var func = contract.GetFunction("getRequest");
			var funcRet = await func.CallDeserializingToObjectAsync<ExchangeRequestByIndexResult>(requestIndex);

			return new ExchangeRequestData() {
				RequestIndex = requestIndex,
				Payload = funcRet.Payload,
				Address = funcRet.Address,
				UserId = funcRet.UserId,
				IsBuyRequest = funcRet.IsBuyRequest,
				IsPending = funcRet.State == 0,
				IsSucceeded = funcRet.State == 1,
				IsCancelled = funcRet.State == 2,
			};
		}

		// ---

		[FunctionOutput]
		private class ExchangeRequestByIndexResult {

			[Parameter("address", 1)]
			public string Address { get; set; }

			[Parameter("string", 2)]
			public string UserId { get; set; }

			[Parameter("string", 3)]
			public string Payload { get; set; }

			[Parameter("bool", 4)]
			public bool IsBuyRequest { get; set; }

			[Parameter("uint8", 5)]
			public int State { get; set; }
		}

	}
}
