using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.Common.Sumus;
using Goldmint.CoreLogic.Services.Blockchain.Sumus;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using Goldmint.DAL;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class SumusEmitter : BaseWorker {

		private readonly int _rowsPerRound;

		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private ISumusWriter _sumusWriter;
		private Goldmint.Common.Sumus.Signer _emitterSigner;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		// ---

		public SumusEmitter(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			var appConfig = services.GetRequiredService<AppConfig>();
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_sumusWriter = services.GetRequiredService<ISumusWriter>();

			// _emitterSigner = new Signer(appConfig.Services.Sumus.MigrationEmitterPk);
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
					from r in _dbContext.MigrationEthereumToSumusRequest
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

				SentTransaction sumTransaction = null;

				if (row.Amount != null && Pack58.Unpack(row.SumAddress, out var sumusAddressBytes)) {
					SumusToken stoken = 0;
					switch (row.Asset) {
						case MigrationRequestAsset.Gold: 
							stoken = SumusToken.Gold;
							break;
						case MigrationRequestAsset.Mnt: 
							stoken = SumusToken.Mnt;
							break;
					}
					sumTransaction = await _sumusWriter.TransferToken(
						_emitterSigner.PrivateKeyBytes, 
						_emitterSigner.NextNonce(), 
						sumusAddressBytes, 
						stoken, 
						row.Amount ?? 0M
					);
				}

				if (sumTransaction != null) {
					row.Status = MigrationRequestStatus.Completed;
					row.SumTransaction = sumTransaction.Hash;
					row.TimeCompleted = DateTime.UtcNow;

					// TODO: check?
					// row.Status = MigrationRequestStatus.EmissionConfirmation;
					// row.SumTransaction = sumTransaction;
					// row.TimeNextCheck = DateTime.UtcNow.Add(nextCheckDelay);
				}
				else {
					row.Status = MigrationRequestStatus.Failed;
					row.TimeCompleted = DateTime.UtcNow;
				}
				await _dbContext.SaveChangesAsync();

				if (sumTransaction != null) {
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
