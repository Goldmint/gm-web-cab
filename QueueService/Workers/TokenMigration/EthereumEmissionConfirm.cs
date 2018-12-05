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

	public class EthereumEmissionConfirm : BaseWorker {

		private readonly int _rowsPerRound;
		private readonly int _confirmationsRequired;

		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		// ---

		public EthereumEmissionConfirm(int rowsPerRound, int confirmationsRequired) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
			_confirmationsRequired = Math.Max(2, confirmationsRequired);
		}

		protected override Task OnInit(IServiceProvider services) {
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
					from r in _dbContext.MigrationSumusToEthereumRequest
					where
						r.Status == MigrationRequestStatus.EmissionConfirmation &&
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

				var info = await _ethereumReader.CheckTransaction(row.EthTransaction, _confirmationsRequired);

				// not found / failed
				if (info == null || info.Status == EthTransactionStatus.Failed) {
					row.Status = MigrationRequestStatus.Failed;
					row.TimeCompleted = DateTime.UtcNow;

					++_statFailed;
					_logger.Error($"Request {row.Id} - emission failed");
				}
				// success
				else if (info.Status == EthTransactionStatus.Success) {
					row.Status = MigrationRequestStatus.Completed;
					row.TimeCompleted = DateTime.UtcNow;

					++_statProcessed;
					_logger.Info($"Request {row.Id} - emission confirmed");
				}
				// pending
				else {
					row.TimeNextCheck = DateTime.UtcNow.AddSeconds(30);
					_logger.Info($"Request {row.Id} - emission is still pending");
				}
				await _dbContext.SaveChangesAsync();
			}
		}
	}
}
