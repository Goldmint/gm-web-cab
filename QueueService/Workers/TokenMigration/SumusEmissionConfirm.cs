using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Sumus;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class SumusEmissionConfirm : BaseWorker {

		private readonly int _rowsPerRound;

		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private ISumusReader _sumusReader;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		// ---

		public SumusEmissionConfirm(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_sumusReader = services.GetRequiredService<ISumusReader>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
					from r in _dbContext.MigrationEthereumToSumusRequest
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

				var info = await _sumusReader.GetTransactionInfo(row.SumTransaction);

				// not found / failed
				if (info == null || info.Status == SumusTransactionStatus.Failed) {
					row.Status = MigrationRequestStatus.Failed;
					row.TimeCompleted = DateTime.UtcNow;

					++_statFailed;
					_logger.Error($"Request {row.Id} - emission failed");
				}
				// success
				else if (info.Status == SumusTransactionStatus.Success) {
					row.Status = MigrationRequestStatus.Completed;
					row.TimeCompleted = DateTime.UtcNow;

					++_statProcessed;
					_logger.Info($"Request {row.Id} - emission confirmed");
				}
				// pending
				else {
					row.TimeNextCheck = DateTime.UtcNow.AddSeconds(15);
					_logger.Info($"Request {row.Id} - emission is still pending");
				}
				await _dbContext.SaveChangesAsync();
			}
		}
	}
}
