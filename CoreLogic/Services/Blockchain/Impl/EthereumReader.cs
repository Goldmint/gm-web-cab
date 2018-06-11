using Goldmint.Common;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using NLog;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Goldmint.CoreLogic.Services.Blockchain.Models;
using Goldmint.CoreLogic.Services.Blockchain.Models.ContractEvent;

namespace Goldmint.CoreLogic.Services.Blockchain.Impl {

	public class EthereumReader : EthereumBaseClient, IEthereumReader {

		public EthereumReader(AppConfig appConfig, LogFactory logFactory) : base(appConfig, logFactory) {
		}

		// ---

		public async Task<BigInteger> GetCurrentGasPrice() {
			return (await GasPrice()).Value;
		}

		public async Task<TransactionInfo> CheckTransaction(string txid, int confirmationsRequired) {

			if (string.IsNullOrWhiteSpace(txid)) {
				throw new ArgumentException("Invalid transaction format");
			}

			var web3 = new Web3(EthProvider);
			var txinfo = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txid);

			if (txinfo != null) {
				var lastBlockNum = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
				var threshold = BigInteger.One * Math.Max(2, confirmationsRequired);

				if (
					txinfo.BlockNumber.HexValue != null && // got into block
					lastBlockNum.Value - txinfo.BlockNumber.Value >= threshold // wait for number of confirmation
				) {
					var txBlockInfo = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(txinfo.BlockNumber);
					var blockTimestamp = (DateTime?) null;
					if (txBlockInfo?.Timestamp != null && txBlockInfo.Timestamp.Value > 0) {
						blockTimestamp = DateTimeOffset.FromUnixTimeSeconds((long)txBlockInfo.Timestamp.Value).UtcDateTime;
					}

					// check status
					if ((txinfo.Status?.Value ?? BigInteger.Zero) == BigInteger.One) {
						return new TransactionInfo() {
							Status = EthTransactionStatus.Success,
							Time = blockTimestamp,
						};
					}
					return new TransactionInfo() {
						Status = EthTransactionStatus.Failed,
						Time = blockTimestamp,
					};
				}
				return new TransactionInfo() {
					Status = EthTransactionStatus.Pending,
				};
			}

			// assume it is pending
			return new TransactionInfo() {
				Status = EthTransactionStatus.Pending,
			};
		}

		public async Task<BigInteger> GetAddressMntBalance(string address) {

			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}

			var web3 = new Web3(EthProvider);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
				MntpTokenContractAddress
			);
			var func = contract.GetFunction("balanceOf");
			var funcRet = await func.CallAsync<BigInteger>(address);

			return funcRet;
		}

		public async Task<BigInteger> GetAddressGoldBalance(string address) {

			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}

			var web3 = new Web3(EthProvider);

			var contract = web3.Eth.GetContract(
				"[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"}]",
				GoldTokenContractAddress
			);
			var func = contract.GetFunction("balanceOf");
			var funcRet = await func.CallAsync<BigInteger>(address);

			return funcRet;
		}

		public async Task<BigInteger> GetHotWalletGoldBalance(string userId) {

			if (string.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("Invalid user ID");
			}

			var web3 = new Web3(EthProvider);
			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			var func = contract.GetFunction("getUserHotGoldBalance");
			var funcRet = await func.CallAsync<BigInteger>(userId);

			return funcRet;
		}
		
		// ---

		public async Task<BigInteger> GetBuySellRequestsCount() {

			var web3 = new Web3(EthProvider);

			var contract = web3.Eth.GetContract(
					FiatContractAbi,
					FiatContractAddress
				);
			var func = contract.GetFunction("getRequestsCount");
			return await func.CallAsync<BigInteger>();
		}

		public async Task<BuySellRequestBaseInfo> GetBuySellRequestBaseInfo(BigInteger requestIndex) {

			var web3 = new Web3(EthProvider);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);
			var func = contract.GetFunction("getRequestBaseInfo");
			var funcRet = await func.CallDeserializingToObjectAsync<BuySellRequestBaseInfoMapping>(requestIndex);

			return new BuySellRequestBaseInfo() {
				Address = funcRet.Address,
				InputAmount = funcRet.InputAmount,
				OutputAmount = funcRet.OutputAmount,
				IsPending = funcRet.State == 0,
				IsSucceeded = funcRet.State == 1,
				IsCancelled = funcRet.State == 2,
				IsFailed = funcRet.State == 3,
			};
		}

		public async Task<GatheredTokenBuyEvents> GatherTokenBuyEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired) {

			var web3 = new Web3(EthLogsProvider);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			var hexLaxtestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			var latestConfirmedBlock = hexLaxtestBlock.Value -= confirmationsRequired;

			var hexFromBlock = new HexBigInteger(BigInteger.Min(from, latestConfirmedBlock));
			var hexToBlock = new HexBigInteger(BigInteger.Min(to, latestConfirmedBlock));

			var evnt = contract.GetEvent("TokenBuyRequest");
			var filter = await evnt.CreateFilterBlockRangeAsync(
				new BlockParameter(hexFromBlock),
				new BlockParameter(hexToBlock)
			);

			var events = new List<TokenBuyRequest>();
			var logs = await evnt.GetAllChanges<TokenBuyRequestEventMapping>(filter);

			foreach (var v in logs) {
				if (!v.Log.Removed) {
					events.Add(new TokenBuyRequest() {
						Address = v.Event.From,
						UserId = v.Event.UserId,
						Reference = v.Event.Reference,
						Amount = v.Event.Amount,
						RequestIndex = v.Event.Index,
						BlockNumber = v.Log.BlockNumber,
						TransactionId = v.Log.TransactionHash,
					});
				}
			}

			return new GatheredTokenBuyEvents() {
				FromBlock = hexFromBlock.Value,
				ToBlock = hexToBlock.Value,
				Events = events.ToArray(),
			};
		}

		public async Task<GatheredTokenSellEvents> GatherTokenSellEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired) {

			var web3 = new Web3(EthLogsProvider);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			var hexLaxtestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			var latestConfirmedBlock = hexLaxtestBlock.Value -= confirmationsRequired;

			var hexFromBlock = new HexBigInteger(BigInteger.Min(from, latestConfirmedBlock));
			var hexToBlock = new HexBigInteger(BigInteger.Min(to, latestConfirmedBlock));

			var evnt = contract.GetEvent("TokenSellRequest");
			var filter = await evnt.CreateFilterBlockRangeAsync(
				new BlockParameter(hexFromBlock),
				new BlockParameter(hexToBlock)
			);

			var events = new List<TokenSellRequest>();
			var logs = await evnt.GetAllChanges<TokenSellRequestEventMapping>(filter);

			foreach (var v in logs) {
				if (!v.Log.Removed) {
					events.Add(new TokenSellRequest() {
						Address = v.Event.From,
						UserId = v.Event.UserId,
						Reference = v.Event.Reference,
						Amount = v.Event.Amount,
						RequestIndex = v.Event.Index,
						BlockNumber = v.Log.BlockNumber,
						TransactionId = v.Log.TransactionHash,
					});
				}
			}

			return new GatheredTokenSellEvents() {
				FromBlock = hexFromBlock.Value,
				ToBlock = hexToBlock.Value,
				Events = events.ToArray(),
			};
		}

		/*
		public async Task<GatheredRequestProcessedEvents> GatherRequestProcessedEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired) {

			var web3 = new Web3(EthLogsProvider);

			var contract = web3.Eth.GetContract(
				FiatContractAbi,
				FiatContractAddress
			);

			var hexLaxtestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			var latestConfirmedBlock = hexLaxtestBlock.Value -= confirmationsRequired;

			var hexFromBlock = new HexBigInteger(BigInteger.Min(from, latestConfirmedBlock));
			var hexToBlock = new HexBigInteger(BigInteger.Min(to, latestConfirmedBlock));

			var evnt = contract.GetEvent("TokenSellRequest");
			var filter = await evnt.CreateFilterBlockRangeAsync(
				new BlockParameter(hexFromBlock),
				new BlockParameter(hexToBlock)
			);

			var events = new List<RequestProcessed>();
			var logs = await evnt.GetAllChanges<RequestProcessedEventMapping>(filter);

			foreach (var v in logs) {
				if (!v.Log.Removed) {
					events.Add(new RequestProcessed() {
						RequestIndex = v.Event.Index,
						IsBuyRequest = v.Event.IsBuyRequest,
						IsFiat = v.Event.IsFiat,
						Amount = v.Event.Amount,
						Rate = v.Event.Rate,
						BlockNumber = v.Log.BlockNumber,
						TransactionId = v.Log.TransactionHash,
					});
				}
			}

			return new GatheredRequestProcessedEvents() {
				FromBlock = hexFromBlock.Value,
				ToBlock = hexToBlock.Value,
				Events = events.ToArray(),
			};
		}
		*/
		
		// ---

		[FunctionOutput]
		internal class BuySellRequestBaseInfoMapping {

			[Parameter("address", 1)]
			public string Address { get; set; }

			[Parameter("uint8", 2)]
			public int State { get; set; }

			[Parameter("uint", 3)]
			public BigInteger InputAmount { get; set; }

			[Parameter("uint", 4)]
			public BigInteger OutputAmount { get; set; }
		}

		internal class TokenBuyRequestEventMapping {

			[Parameter("address", "_from", 1, false)]
			public string From { get; set; }

			[Parameter("string", "_userId", 2, false)]
			public string UserId { get; set; }

			[Parameter("uint", "_reference", 3, false)]
			public BigInteger Reference { get; set; }

			[Parameter("uint", "_amount", 4, false)]
			public BigInteger Amount { get; set; }

			[Parameter("uint", "_index", 5, true)]
			public BigInteger Index { get; set; }
		}

		internal class TokenSellRequestEventMapping {

			[Parameter("address", "_from", 1, false)]
			public string From { get; set; }

			[Parameter("string", "_userId", 2, false)]
			public string UserId { get; set; }

			[Parameter("uint", "_reference", 3, false)]
			public BigInteger Reference { get; set; }

			[Parameter("uint", "_amount", 4, false)]
			public BigInteger Amount { get; set; }

			[Parameter("uint", "_index", 5, true)]
			public BigInteger Index { get; set; }
		}

		/*internal class RequestProcessedEventMapping {

			[Parameter("uint", "_index", 1, ???)]
			public BigInteger Index { get; set; }

			[Parameter("bool", "_isBuyRequest", 2, false)]
			public bool IsBuyRequest { get; set; }

			[Parameter("bool", "_isFiat", 3, false)]
			public bool IsFiat { get; set; }

			[Parameter("uint", "_amount", 4, false)]
			public BigInteger Amount { get; set; }

			[Parameter("uint", "_rate", 5, false)]
			public BigInteger Rate { get; set; }
		}*/
	}
}
