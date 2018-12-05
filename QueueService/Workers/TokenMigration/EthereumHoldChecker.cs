using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.DAL;
using Goldmint.DAL.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class EthereumHoldChecker : BaseWorker {

		private readonly int _blocksPerRound;
		private readonly int _confirmationsRequired;

		private ILogger _logger;
		private AppConfig _appConfig;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private BigInteger _lastBlock = BigInteger.Zero;
		private BigInteger _lastSavedBlock = BigInteger.Zero;
		private long _statProcessed = 0;

		// ---

		public EthereumHoldChecker(int blocksPerRound, int confirmationsRequired) {
			_blocksPerRound = Math.Max(1, blocksPerRound);
			_confirmationsRequired = Math.Max(2, confirmationsRequired);
		}

		protected override async Task OnInit(IServiceProvider services) {
			_appConfig = services.GetRequiredService<AppConfig>();
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();

			// get last block from db
			if (BigInteger.TryParse(await _dbContext.GetDbSetting(DbSetting.MigrationEthHarvLastBlock, null), out var lbDb) && lbDb >= 0) {
				_lastBlock = lbDb;
				_lastSavedBlock = lbDb;
				_logger.Info($"Using last block #{lbDb} (DB)");
			}
			else {
				_lastBlock = await _ethereumReader.GetLogsLatestBlockNumber();
				_logger.Info($"Using last block #{_lastBlock} (logs provider)");
			}
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			// get events
			var log = await _ethereumReader.GatherMigrationContractTransfers(_lastBlock, _lastBlock + _blocksPerRound, _confirmationsRequired);
			_lastBlock = log.ToBlock;

			_logger.Debug(
				(log.Events.Length > 0
					? $"{log.Events.Length} request(s) found"
					: "Nothing found"
				) + $" in blocks [{log.FromBlock} - {log.ToBlock}]"
			);

			if (IsCancelled()) return;
			
			foreach (var v in log.Events) {

				if (IsCancelled()) return;
				_dbContext.DetachEverything();

				_logger.Debug($"Trying to process transfer at {v.Transaction}");

				if (v.Amount <= 0) {
					_logger.Debug($"Invalid amount of transfer at {v.Transaction}");
					continue;
				}

				MigrationRequestAsset asset;

				// gold
				if (v.TokenContractAddress == _appConfig.Services.Ethereum.GoldContractAddress) {
					asset = MigrationRequestAsset.Gold;
				}
				// mint
				else if (v.TokenContractAddress == _appConfig.Services.Ethereum.MntpContractAddress) {
					asset = MigrationRequestAsset.Mnt;
				}
				// unknown
				else {
					_logger.Debug($"Unknown contract address {v.TokenContractAddress} at {v.Transaction}");
					continue;
				}

				// find row
				var row = await (
						from r in _dbContext.MigrationEthereumToSumusRequest
						where
							r.Asset == asset &&
							r.Status == MigrationRequestStatus.TransferConfirmation &&
							r.EthAddress == v.From
						select r
					)
					.AsTracking()
					.FirstOrDefaultAsync()
				;
				
				// not found
				if (row == null) {
					_logger.Debug($"Transfer at {v.Transaction} not found in DB (or previously processed)");
					continue;
				}

				// cast from bigint
				var decAmount = 0m;
				var longBlock = 0UL;
				try {
					decAmount = v.Amount.FromEther();
					longBlock = (ulong) v.BlockNumber;
				}
				catch (Exception e) {
					_logger.Error(e, $"Transfer at {v.Transaction} has too big amount/blocknum value for decimal/long type");
					continue;
				}

				// update
				row.Status = MigrationRequestStatus.Emission;
				row.Amount = decAmount;
				row.Block = longBlock;
				row.EthTransaction = v.Transaction;
				row.TimeNextCheck = DateTime.UtcNow.AddSeconds(0);

				// save
				try {
					await _dbContext.SaveChangesAsync();
				}
				catch (Exception e) when (e.IsMySqlDuplicateException()) {
					_logger.Debug($"Transfer at {v.Transaction} is already processed");
					continue;
				}

				_logger.Info($"Transfer at {v.Transaction} is processed");
				++_statProcessed;
			}

			// save last index to settings
			if (_lastSavedBlock != _lastBlock) {
				if (await _dbContext.SaveDbSetting(DbSetting.MigrationEthHarvLastBlock, _lastBlock.ToString())) {
					_lastSavedBlock = _lastBlock;
					_logger.Info($"Last block {_lastBlock} is saved to DB");
				}
			}
		}
	}
}
