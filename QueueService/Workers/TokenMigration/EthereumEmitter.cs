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
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class EthereumEmitter : BaseWorker {

		private readonly int _rowsPerRound;

		private ILogger _logger;
		private AppConfig _appConfig;
		private ApplicationDbContext _dbContext;
		private IEthereumWriter _ethereumWriter;
		private INotificationQueue _notificationQueue;
		private ITemplateProvider _templateProvider;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		// ---

		public EthereumEmitter(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_appConfig = services.GetRequiredService<AppConfig>();
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			_notificationQueue = services.GetRequiredService<INotificationQueue>();
			_templateProvider = services.GetRequiredService<ITemplateProvider>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
					from r in _dbContext.MigrationSumusToEthereumRequest
					where
						r.Status == MigrationRequestStatus.Emission &&
						r.TimeNextCheck <= nowTime
					select r
				)
				.Include(_ => _.User)
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
					row.TimeNextCheck = DateTime.UtcNow.AddSeconds(30);
				}
				else {
					row.Status = MigrationRequestStatus.Failed;
					row.TimeCompleted = DateTime.UtcNow;
				}
				await _dbContext.SaveChangesAsync();

				// notify
				if (ethTransaction != null) {
					try {
						await EmailComposer
							.FromTemplate(await _templateProvider.GetEmailTemplate(EmailTemplate.ExchangeEthTransferred, Locale.En))
							.ReplaceBodyTag("REQUEST_ID", row.Id.ToString())
							.ReplaceBodyTag("TOKEN", row.Asset.ToString().ToUpperInvariant())
							.ReplaceBodyTag("LINK", _appConfig.Services.Ethereum.EtherscanTxView + ethTransaction)
							.ReplaceBodyTag("DETAILS_SOURCE", TextFormatter.MaskBlockchainAddress(row.SumAddress))
							.ReplaceBodyTag("DETAILS_AMOUNT", row.Amount.Value.ToString("F"))
							.ReplaceBodyTag("DETAILS_DESTINATION", TextFormatter.MaskBlockchainAddress(row.EthAddress))
							.Send(row.User.Email, row.User.UserName, _notificationQueue)
						;
					} catch { }
				}

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
