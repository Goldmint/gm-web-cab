using Goldmint.Common;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models.ContractEvent;
using Serilog;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl {

	public class EthereumReader : EthereumBaseClient, IEthereumReader {

		public EthereumReader(AppConfig appConfig, ILogger logFactory) : base(appConfig, logFactory) {
		}

		public async Task<BigInteger> GetCurrentGasPrice() {
			return (await GasPrice()).Value;
		}

		public async Task<BigInteger> GetLogsLatestBlockNumber() {
			var web3 = new Web3(EthLogsProvider);

			var syncResp = await web3.Eth.Syncing.SendRequestAsync();
			if (syncResp.IsSyncing) {
				return syncResp.CurrentBlock;
			}
			return await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
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

		public async Task<BigInteger> GetEtherBalance(string address) {
			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}
			var web3 = new Web3(EthProvider);

			var b = await web3.Eth.GetBalance.SendRequestAsync(address);
			return b.Value;
		}

		public async Task<BigInteger> GetAddressMntBalance(string address) {
			if (string.IsNullOrWhiteSpace(address)) {
				throw new ArgumentException("Invalid address format");
			}
			var web3 = new Web3(EthProvider);
			var contract = web3.Eth.GetContract(MntpContractAbi, MntpContractAddress);
			var func = contract.GetFunction("balanceOf");
			var funcRet = await func.CallAsync<BigInteger>(address);
			return funcRet;
		}
		
		public async Task<GatheredPoolFreezerEvents> GatherPoolFreezerEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired) {
			
			var web3 = new Web3(EthLogsProvider);
			
			var contract = web3.Eth.GetContract(
				PoolFreezerContractAbi,
				PoolFreezerContractAddress
			);

			HexBigInteger hexLaxtestBlock;
			var syncResp = await web3.Eth.Syncing.SendRequestAsync();
			if (syncResp.IsSyncing) {
				hexLaxtestBlock = syncResp.CurrentBlock;
			}
			else {
				hexLaxtestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			}

			var latestConfirmedBlock = hexLaxtestBlock.Value -= confirmationsRequired;
			var hexFromBlock = new HexBigInteger(BigInteger.Min(from, latestConfirmedBlock));
			var hexToBlock = new HexBigInteger(BigInteger.Min(to, latestConfirmedBlock));

			var evt = contract.GetEvent("onFreeze");

			var evtFilter = evt.CreateFilterInput(
				new BlockParameter(hexFromBlock),
				new BlockParameter(hexToBlock)
			);
			var logs = await evt.GetAllChanges<PoolFreezeEventDTO>(evtFilter);

			var events = new List<PoolFreezeEvent>();
			foreach (var v in logs) {
				if (!v.Log.Removed) {
					try {
						var sumusAddr58 = Common.Sumus.Pack58.Pack(v.Event.SumusAddress);

						events.Add(new PoolFreezeEvent() {
							Address = v.Event.UserAddress,
							Amount = v.Event.TokenAmount,
							SumusAddress = sumusAddr58,
							BlockNumber = v.Log.BlockNumber,
							TransactionId = v.Log.TransactionHash,
						});
					} catch (Exception e) {
						Logger.Error(e, "Failed to pack Sumus address. Event skipped");
					}
				}
			}

			return new GatheredPoolFreezerEvents() {
				FromBlock = hexFromBlock.Value,
				ToBlock = hexToBlock.Value,
				Events = events.ToArray(),
			};
		}

		// ---
		
		[Event("onFreeze")]
		public sealed class PoolFreezeEventDTO : IEventDTO {

			[Parameter("address", "userAddress", 1, true)]
			public string UserAddress { get; set; }

			[Parameter("uint", "tokenAmount", 2, false)]
			public BigInteger TokenAmount { get; set; }

			[Parameter("bytes32", "sumusAddress", 3, false)]
			public byte[] SumusAddress { get; set; }
		}
	}
}
