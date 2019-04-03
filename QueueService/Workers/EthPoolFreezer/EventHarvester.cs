using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.DAL.Extensions;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public sealed class EventHarvester : BaseWorker {

		private readonly int _blocksPerRound;
		private readonly int _confirmationsRequired;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private BigInteger _lastBlock;
		private BigInteger _lastSavedBlock;

		private long _statProcessed = 0;

		public EventHarvester(int blocksPerRound, int confirmationsRequired) {
			_blocksPerRound = Math.Max(1, blocksPerRound);
			_confirmationsRequired = Math.Max(2, confirmationsRequired);
			_lastBlock = BigInteger.Zero;
			_lastSavedBlock = BigInteger.Zero;
		}

		protected override async Task OnInit(IServiceProvider services) {

			Logger.Info($"{_confirmationsRequired} confirmations required for pool-freezer event");

			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();
			var runtimeConfig = services.GetRequiredService<RuntimeConfigHolder>().Clone();

			// get last block from config
			if (BigInteger.TryParse(runtimeConfig.Ethereum.HarvestFromBlock, out var lbCfg) && lbCfg >= 0) {
				_lastBlock = lbCfg;

				Logger.Info($"Using last block #{lbCfg} (appsettings)");
			}

			// get last block from db; remember last saved block
			if (BigInteger.TryParse(await _dbContext.GetDbSetting(DbSetting.PoolFreezerHarvLastBlock, "0"), out var lbDb) && lbDb >= 0 && lbDb >= lbCfg) {
				_lastBlock = lbDb;
				_lastSavedBlock = lbDb;

				Logger.Info($"Using last block #{lbDb} (DB)");
			}
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			// get events
			var log = await _ethereumReader.GatherPoolFreezerEvents(_lastBlock - 1, _lastBlock + _blocksPerRound, _confirmationsRequired);
			_lastBlock = log.ToBlock;

			Logger.Debug(
				(log.Events.Length > 0
					? $"{log.Events.Length} request(s) found"
					: "Nothing found"
				) + $" in blocks [{log.FromBlock} - {log.ToBlock}]"
			);

			if (IsCancelled()) return;

			foreach (var v in log.Events) {

				if (IsCancelled()) return;
				Logger.Debug($"Trying to enqueue pool-freezer event with tx {v.TransactionId}");

				try {
					var model = new DAL.Models.PoolFreezeRequest() {
						Status = EmissionRequestStatus.Initial,
						EthAddress = v.Address,
						EthTransaction = v.TransactionId,
						Amount = v.Amount.FromSumus(),
						SumAddress = v.SumusAddress,
						SumTransaction = null,
						TimeCreated = DateTime.UtcNow,
						TimeCompleted = null,
					};
					_dbContext.PoolFreezeRequest.Add(model);
					await _dbContext.SaveChangesAsync();

					Logger.Info(
						$"Pool-freezer event enqueued for tx {v.TransactionId}"
					);
				} catch (Exception e) {
					if (!ExceptionExtension.IsMySqlDuplicateException(e)) {
						Logger.Error(e, $"Pool-freezer event failed with tx {v.TransactionId}");
					}
				}

				++_statProcessed;
			}

			// save last index to settings
			if (_lastSavedBlock != _lastBlock) {
				if (await _dbContext.SaveDbSetting(DbSetting.PoolFreezerHarvLastBlock, _lastBlock.ToString())) {
					_lastSavedBlock = _lastBlock;
					Logger.Info($"Last block #{_lastBlock} saved to DB");
				}
			}
		}
	}
}
