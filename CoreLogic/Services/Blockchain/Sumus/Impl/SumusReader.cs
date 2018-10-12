using Goldmint.Common;
using Goldmint.DAL;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using Goldmint.Common.Extensions;
using System.Threading.Tasks;
using Goldmint.Common.WebRequest;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Impl {

	public sealed class SumusReader : ISumusReader {

		private readonly LogFactory _logFactory;
		private readonly ILogger _logger;
		private readonly ScannerDbContext _dbContext;
		private readonly AppConfig _appConfig;

		public SumusReader(ScannerDbContext dbContext, AppConfig appConfig, LogFactory logFactory) {
			_logFactory = logFactory;
			_logger = logFactory.GetLoggerFor(this.GetType());
			_dbContext = dbContext;
			_appConfig = appConfig;
		}

		public async Task<TransactionInfo> GetTransactionInfo(string hash, DateTime? postedAtTime) {

			var tx = await (
					from r in _dbContext.Transaction
					where
						r.UniqueId == hash
					select r
				)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (tx != null) {
				if (!Common.Sumus.Token.ParseToken(tx.TokenType, out var token)) {
					throw new Exception("Failed to parse sumus token type from string");
				}

				return new TransactionInfo() {
					Status = SumusTransactionStatus.Success,
					Data = new TransactionData() {
						Id = tx.TransactionId,
						Hash = tx.UniqueId,
						BlockNumber = tx.BlockNumber,

						From = tx.SourceWallet,
						To = tx.DestinationWallet,
						Token = token,
						TokenAmount = tx.TokensCount,

						TimeStamp = tx.TimeStamp,
					},
				};
			}

			if (postedAtTime != null && DateTime.UtcNow - postedAtTime < TimeSpan.FromMinutes(5)) {
				return new TransactionInfo() {
					Status = SumusTransactionStatus.Pending
				};
			}

			return new TransactionInfo() {
				Status = SumusTransactionStatus.Failed
			};
		}

		public async Task<List<TransactionInfo>> GetBlocksSpanTransaction(string destinationWallet, ulong beginBlock, ulong endBlock) {
			var txs = await (
					from r in _dbContext.Transaction
					where
						r.DestinationWallet == destinationWallet &&
						r.BlockNumber >= beginBlock &&
						r.BlockNumber <= endBlock
					select r
				)
				.AsNoTracking()
				.ToListAsync()
			;

			var ret = new List<TransactionInfo>();
			foreach (var tx in txs) {
				if (!Common.Sumus.Token.ParseToken(tx.TokenType, out var token)) {
					throw new Exception("Failed to parse sumus token type from string");
				}

				ret.Add(new TransactionInfo() {
					Status = SumusTransactionStatus.Success,
					Data = new TransactionData() {
						Id = tx.TransactionId,
						Hash = tx.UniqueId,
						BlockNumber = tx.BlockNumber,

						From = tx.SourceWallet,
						To = tx.DestinationWallet,
						Token = token,
						TokenAmount = tx.TokensCount,

						TimeStamp = tx.TimeStamp,
					},
				});
			}

			return ret;
		}

		public async Task<ulong> GetLastBlockNumber()
		{
			return await _dbContext.Block.MaxAsync(_ => _.Number);
		}

		public async Task<WalletState> GetWalletState(string addr)
		{
			var url = string.Format("{0}/wallet/{1}", _appConfig.Services.Sumus.SumusNodeProxyUrl, addr);
			var res = await SumusNodeProxy.Get<ProxyWalletStateResult>(url, _logger);
			if (res == null) {
				return null;
			}

			if (!decimal.TryParse(res.balance.gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var goldDec)) {
				throw new Exception("Failed to parse GOLD token amount");
			}
			if (!decimal.TryParse(res.balance.gold, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mntDec)) {
				throw new Exception("Failed to parse MNT token amount");
			}

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
			public ProxyWalletStateResultBalance balance { get;set; }
			public bool exist { get;set; }
			public ulong approved_nonce { get;set; }
			public string[] tags { get;set; }
		}

		internal class ProxyWalletStateResultBalance {
			public string gold { get;set; }
			public string mint { get;set; }
		}
	}
}
