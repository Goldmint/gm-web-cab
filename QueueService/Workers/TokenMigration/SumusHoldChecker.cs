using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Blockchain.Sumus;
using Goldmint.DAL;
using Goldmint.DAL.Extensions;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class SumusHoldChecker : BaseWorker {

		private readonly int _blocksPerRound;

		private ILogger _logger;
		private AppConfig _appConfig;
		private ApplicationDbContext _dbContext;
		private ISumusReader _sumusReader;

		private BigInteger _lastBlock = 0L;
		private BigInteger _lastSavedBlock = 0L;
		private long _statProcessed = 0;

		// ---

		public SumusHoldChecker(int blocksPerRound) {
			_blocksPerRound = Math.Max(1, blocksPerRound);
		}

		protected override async Task OnInit(IServiceProvider services) {
			_appConfig = services.GetRequiredService<AppConfig>();
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_sumusReader = services.GetRequiredService<ISumusReader>();

			// get last block from db
			if (BigInteger.TryParse(await _dbContext.GetDbSetting(DbSetting.MigrationSumHarvLastBlock, null), out var lbDb) && lbDb >= 0) {
				_lastBlock = lbDb;
				_lastSavedBlock = lbDb;
				_logger.Info($"Using last block #{lbDb} (DB)");
			}
			else {
				_lastBlock = await _sumusReader.GetLastBlockNumber();
				_logger.Info($"Using last block #{_lastBlock} (logs)");
			}
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var maxBlock = await _sumusReader.GetLastBlockNumber();
			var blockFrom = Math.Min((ulong) _lastBlock, maxBlock);
			var blockTo = Math.Min((ulong) (_lastBlock + _blocksPerRound), maxBlock);

			// get transactions
			var transactions = await _sumusReader.GetBlocksSpanTransaction(_appConfig.Services.Sumus.MigrationHolderAddress, blockFrom, blockTo);
			_lastBlock = blockTo;

			_logger.Debug(
				(transactions.Count > 0
					? $"{transactions.Count} request(s) found"
					: "Nothing found"
				) + $" in blocks [{blockFrom} - {blockTo}]"
			);

			if (IsCancelled()) return;

			foreach (var v in transactions) {

				if (IsCancelled()) return;
				_dbContext.DetachEverything();

				_logger.Debug($"Trying to process transfer at {v.Data.Hash}");

				if (v.Data.TokenAmount <= 0) {
					_logger.Debug($"Invalid amount of transfer at {v.Data.Hash}");
					continue;
				}

				MigrationRequestAsset asset;

				// gold
				if (v.Data.Token == SumusToken.Gold) asset = MigrationRequestAsset.Gold;
				// mint
				else if (v.Data.Token == SumusToken.Mnt) asset = MigrationRequestAsset.Mnt;
				// unknown
				else {
					_logger.Debug($"Unsupported token type {v.Data.Token} at {v.Data.Hash}");
					continue;
				}

				// find row
				var row = await (
						from r in _dbContext.MigrationSumusToEthereumRequest
						where
							r.Asset == asset &&
							r.Status == MigrationRequestStatus.TransferConfirmation &&
							r.SumAddress == v.Data.From
						select r
					)
					.AsTracking()
					.FirstOrDefaultAsync()
				;

				// not found
				if (row == null) {
					_logger.Debug($"Transfer at {v.Data.Hash} not found in DB (or previously processed)");
					continue;
				}

				// update
				row.Status = MigrationRequestStatus.Emission;
				row.Amount = v.Data.TokenAmount;
				row.Block = v.Data.BlockNumber;
				row.SumTransaction = v.Data.Hash;
				row.TimeNextCheck = DateTime.UtcNow.AddSeconds(0);

				// save
				try {
					await _dbContext.SaveChangesAsync();
				}
				catch (Exception e) when (e.IsMySqlDuplicateException()) {
					_logger.Debug($"Transfer at {v.Data.Hash} is already processed");
					continue;
				}

				_logger.Info($"Transfer at {v.Data.Hash} is processed");
				++_statProcessed;
			}

			// save last index to settings
			if (_lastSavedBlock != _lastBlock) {
				if (await _dbContext.SaveDbSetting(DbSetting.MigrationSumHarvLastBlock, _lastBlock.ToString())) {
					_lastSavedBlock = _lastBlock;
					_logger.Info($"Last block {_lastBlock} is saved to DB");
				}
			}
		}
	}
}
