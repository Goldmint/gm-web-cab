using Goldmint.Common;
using Goldmint.DAL;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Numerics;
using Goldmint.Common.Extensions;
using System.Threading.Tasks;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using Microsoft.EntityFrameworkCore;
using Goldmint.Common.Sumus;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Impl {

	public sealed class SumusReader : ISumusReader {

		private readonly LogFactory _logFactory;
		private readonly ILogger _logger;
		private readonly AppConfig _appConfig;

		public SumusReader(AppConfig appConfig, LogFactory logFactory) {
			_logFactory = logFactory;
			_logger = logFactory.GetLoggerFor(this.GetType());
			_appConfig = appConfig;
		}

		public async Task<TransactionInfo> GetTransactionInfo(string digest) {

			var url = string.Format("{0}/tx/{1}", _appConfig.Services.Sumus.SumusNodeProxyUrl, digest);
			var res = await SumusNodeProxy.Get<ProxyTransactionInfoResult>(url, _logger);
			if (res == null) {
				throw new Exception("Failed to get transaction info");
			}

			if (res.status == "notfound" || res.transaction == null) {
				return null;
			}

			var status = SumusTransactionStatus.Failed;
			if (res.status == "pending") {
				status = SumusTransactionStatus.Pending;
			}
			else if (res.status == "stale") {
				status = SumusTransactionStatus.Stale;
			}

			var amount = 0m;
			var token = SumusToken.Gold;
			{
				var gold= decimal.Parse(res.transaction.amount_gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
				var mnt = decimal.Parse(res.transaction.amount_mnt, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
				if (mnt > 0) {
					amount = mnt;
					token = SumusToken.Mnt;
				}
				else {
					amount = gold;
					token = SumusToken.Gold;
				}
			}

			return new TransactionInfo() {
				Status = status,
				Data = new TransactionData() {
					Digest = res.transaction.digest,
					BlockNumber = ulong.Parse(res.transaction.block, NumberStyles.Integer, CultureInfo.InvariantCulture),
					Token = token,
					TokenAmount = amount,
					From = res.transaction.from,
					To = res.transaction.to,
					TimeStamp = DateTimeOffset.FromUnixTimeSeconds((long)res.transaction.timestamp).UtcDateTime,
				},
			};
		}

		public async Task<List<TransactionInfo>> GetWalletIncomingTransactions(string address, ulong blockId) {
			
			var ret = new List<TransactionInfo>();
			var from = "-";

			var bi = await GetBlockInfo(blockId);
			if (bi == null) {
				throw new Exception("Failed to get block info or it didn't appear on sumus rest");
			}

			while (true) {

				var url = $"{_appConfig.Services.Sumus.SumusNodeProxyUrl}/tx/list/{blockId}/{address}/{@from}";
				var res = await SumusNodeProxy.Get<ProxyTransactionListResult>(url, _logger);
				if (res == null) {
					throw new Exception("Failed to get wallet incoming transactions");
				}

				if ((res.list?.Length ?? 0) == 0) {
					break;
				}

				foreach (var tx in res.list) {
					
					from = tx.transaction.digest;

					if (tx.transaction.to != address || tx.transaction.name != Transaction.TxTransferAsset) {
						continue;
					}

					var amount = 0m;
					var token = SumusToken.Gold;
					{
						var gold= decimal.Parse(tx.transaction.amount_gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
						var mnt = decimal.Parse(tx.transaction.amount_mnt, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
						if (mnt > 0) {
							amount = mnt;
							token = SumusToken.Mnt;
						}
						else {
							amount = gold;
							token = SumusToken.Gold;
						}
					}

					ret.Add(new TransactionInfo() {
						Status = SumusTransactionStatus.Success,
						Data = new TransactionData() {
							Digest = tx.transaction.digest,
							BlockNumber = ulong.Parse(tx.transaction.block, NumberStyles.Integer, CultureInfo.InvariantCulture),
							Token = token,
							TokenAmount = amount,
							From = tx.transaction.from,
							To = tx.transaction.to,
							TimeStamp = DateTimeOffset.FromUnixTimeSeconds((long) tx.transaction.timestamp).UtcDateTime,
						},
					});
				}
			}

			return ret;
		}

		public async Task<ulong> GetBlocksCount() {
			var url = string.Format("{0}/status", _appConfig.Services.Sumus.SumusNodeProxyUrl);
			var res = await SumusNodeProxy.Get<ProxyBlockchainStateResult>(url, _logger);
			if (res == null) {
				throw new Exception("Failed to contact Sumus rest proxy");
			}
			if (res.blockchain_state?.block_count == null) {
				throw new Exception("Failed to get blocks count");
			}
			if (!ulong.TryParse(res.blockchain_state.block_count, out var blocks)) {
				throw new Exception("Failed to parse blocks count");
			}
			return blocks;
		}
		
		public async Task<BlockInfo> GetBlockInfo(ulong id) {
			var url = string.Format("{0}/block/{1}", _appConfig.Services.Sumus.SumusNodeProxyUrl, id);
			var res = await SumusNodeProxy.Get<ProxyBlockInfoResult>(url, _logger);
			if (res == null) {
				return null;
			}

			return new BlockInfo() {
				Id = id,
				Transactions = res.transactions,
				TotalUserData = res.total_user_data,
				TotalGold = decimal.Parse(res.total_gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
				TotalMnt = decimal.Parse(res.total_mnt, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
				FeeGold = decimal.Parse(res.fee_gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
				FeeMnt = decimal.Parse(res.fee_mnt, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
				TimeStamp = DateTimeOffset.FromUnixTimeSeconds((long)res.timestamp).UtcDateTime,
			};
		}

		public async Task<WalletState> GetWalletState(string addr) {
			var url = string.Format("{0}/wallet/{1}", _appConfig.Services.Sumus.SumusNodeProxyUrl, addr);
			var res = await SumusNodeProxy.Get<ProxyWalletStateResult>(url, _logger);
			if (res == null) {
				throw new Exception("Failed to get wallet state");
			}

			var goldDec = decimal.Parse(res.balance.gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
			var mntDec = decimal.Parse(res.balance.mint, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

			return new WalletState() {
				Balance = new WalletState.BalanceData() {
					Gold = goldDec.ToSumus(),
					Mnt = mntDec.ToSumus(),
				},
				Exist = res.exist,
				LastNonce = res.approved_nonce,
				Tags = res.tags,
			};
		}

		// ---

		internal class ProxyWalletStateResult {

			public ProxyWalletStateResultBalance balance { get; set; }
			public bool exist { get; set; }
			public ulong approved_nonce { get; set; }
			public string[] tags { get; set; }

			public class ProxyWalletStateResultBalance {
				public string gold { get; set; }
				public string mint { get; set; }
			}
		}

		internal class ProxyTransactionInfoResult {

			public string status { get; set; }
			public ProxyTransactionInfoDataResult transaction {get; set; }

			public class ProxyTransactionInfoDataResult {
				public string name { get; set; }
				public string digest { get; set; }
				public string from { get; set; }
				public string to { get; set; }
				public ulong nonce { get; set; }
				public string block { get; set; }
				public string amount_mnt { get; set; }
				public string amount_gold { get; set; }
				public ulong data_size { get; set; }
				public string data_piece { get; set; }
				public ulong timestamp { get; set; }
			}
		}
		
		internal class ProxyTransactionListResult {

			public ProxyTransactionInfoResult[] list { get; set; }
		}

		internal class ProxyBlockchainStateResult {

			public BlockchainState blockchain_state {get; set; }

			public class BlockchainState {
				public string block_count { get; set; }
				public string transaction_count { get; set; }
				public string node_count { get; set; }
			}
		}

		internal class ProxyBlockInfoResult {

			public string id { get; set; }
			public ulong transactions { get; set; }
			public ulong total_user_data { get; set; }
			public string total_gold { get; set; }
			public string total_mnt { get; set; }
			public string fee_gold { get; set; }
			public string fee_mnt { get; set; }
			public ulong timestamp { get; set; }
		}
	}
}
