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

		private const int TransactionMinConfirmationsCount = 2;

		public InfuraReader(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {
		}

		public async Task<EthTransactionStatus> CheckTransaction(string transactionId) {

			if (string.IsNullOrWhiteSpace(transactionId)) {
				throw new ArgumentException("Invalid transaction format");
			}

			var web3 = new Web3(JsonRpcClient);
			var txinfo = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionId);

			if (txinfo != null) {

				var lastBlockNum = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
				var threshold = BigInteger.One * TransactionMinConfirmationsCount;

				if (
					txinfo.BlockNumber.HexValue != null && // got into block
					lastBlockNum.Value - txinfo.BlockNumber.Value >= threshold // wait for number of confirmation
				) {
					// check status
					if ((txinfo.Status?.Value ?? BigInteger.Zero) == BigInteger.One) {
						return EthTransactionStatus.Success;
					}
					return EthTransactionStatus.Failed;
				}
				return EthTransactionStatus.Pending;
			}

			// assume it is pending
			return EthTransactionStatus.Pending;
		}

		public async Task<long> GetUserFiatBalance(string userId, FiatCurrency currency) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user ID");
			}

			if (currency == FiatCurrency.USD) {
				var web3 = new Web3(JsonRpcClient);
				var contract = web3.Eth.GetContract(
					FiatContractABI,
					FiatContractAddress
				);

				var func = contract.GetFunction("getUserFiatBalance");
				var funcRet = await func.CallAsync<BigInteger>(userId);

				return funcRet > long.MaxValue? long.MaxValue: (long)funcRet;
			}

			throw new NotImplementedException("Currency not implemented");
		}

		public async Task<BigInteger> GetUserGoldBalance(string userId) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user ID");
			}

			var web3 = new Web3(JsonRpcClient);
			var contract = web3.Eth.GetContract(
				FiatContractABI,
				FiatContractAddress
			);

			var func = contract.GetFunction("getUserHotGoldBalance");
			var funcRet = await func.CallAsync<BigInteger>(userId);

			return funcRet;
		}

		public async Task<BigInteger> GetAddressGoldBalance(string address) {

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

		public async Task<BigInteger> GetAddressMntpBalance(string address) {

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
					FiatContractABI,
					FiatContractAddress
				);
			var func = contract.GetFunction("getRequestsCount");
			return await func.CallAsync<BigInteger>();
		}

		public async Task<ExchangeRequestData> GetExchangeRequestByIndex(BigInteger requestIndex) {

			var web3 = new Web3(JsonRpcClient);

			var contract = web3.Eth.GetContract(
				FiatContractABI,
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
