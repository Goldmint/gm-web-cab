using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class EthereumEmitter : BaseWorker {

		private readonly int _rowsPerRound;
		private readonly int _nextCheckDelay;

		private ILogger _logger;
		private AppConfig _appConfig;
		private ApplicationDbContext _dbContext;
		private IEthereumWriter _ethereumWriter;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		// ---

		public EthereumEmitter(int rowsPerRound, int nextCheckDelay) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
			_nextCheckDelay = Math.Max(1, nextCheckDelay);
		}

		protected override Task OnInit(IServiceProvider services) {
			_appConfig = services.GetRequiredService<AppConfig>();
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;
			var nextCheckDelay = TimeSpan.FromSeconds(_nextCheckDelay);

			var rows = await (
					from r in _dbContext.MigrationSumusToEthereumRequest
					where
						r.Status == MigrationRequestStatus.Emission &&
						r.TimeNextCheck <= nowTime
					select r
				)
				.AsTracking()
				.OrderBy(_ => _.Id)
				.Take(_rowsPerRound)
				.ToArrayAsync()
			;

			if (IsCancelled()) return;

			_logger.Debug(rows.Length > 0 ? $"{rows.Length} request(s) found" : "Nothing found");

			foreach (var row in rows) {

				if (IsCancelled()) return;

				row.Status = MigrationRequestStatus.EmissionStarted;
				await _dbContext.SaveChangesAsync();

				string ethTransaction = null;

				if (row.Amount != null) {
					ethTransaction = await _ethereumWriter.MigrationContractUnholdToken(row.EthAddress, row.Asset, row.Amount.Value.ToEther());
				}

				if (ethTransaction != null) {
					row.Status = MigrationRequestStatus.EmissionConfirmation;
					row.EthTransaction = ethTransaction;
					row.TimeNextCheck = DateTime.UtcNow.Add(nextCheckDelay);
				}
				else {
					row.Status = MigrationRequestStatus.Failed;
					row.TimeCompleted = DateTime.UtcNow;
				}
				await _dbContext.SaveChangesAsync();

				if (ethTransaction != null) {
					++_statProcessed;
					_logger.Info($"Request {row.Id} - emission success");
				}
				else {
					++_statFailed;
					_logger.Error($"Request {row.Id} - emission failed");
				}
			}
		}
	}
}
