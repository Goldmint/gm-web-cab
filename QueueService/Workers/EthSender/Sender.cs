using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification;

namespace Goldmint.QueueService.Workers.EthSender {

	public sealed class Sender : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethReader;
		private IEthereumWriter _ethWriter;
		private AppConfig _appConfig;
		private ITemplateProvider _templateProvider;
		private INotificationQueue _notificationQueue;

		public Sender(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethReader = services.GetRequiredService<IEthereumReader>();
			_ethWriter = services.GetRequiredService<IEthereumWriter>();
			_appConfig = services.GetRequiredService<AppConfig>();
			_templateProvider = services.GetRequiredService<ITemplateProvider>();
			_notificationQueue = services.GetRequiredService<INotificationQueue>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await (
				from r in _dbContext.EthSending
				where r.Status == EthereumOperationStatus.Initial
				select r
			)
			.Include(_ => _.User)
			.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
			.AsTracking()
			.Take(_rowsPerRound)
			.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			var ethAmount = await _ethReader.GetEtherBalance(await _ethWriter.GetEthSender());

			foreach (var r in rows) {
				if (IsCancelled() || ethAmount < r.Amount.ToEther()) {
					return;
				}

				try {
					r.Status = EthereumOperationStatus.BlockchainInit;
					_dbContext.SaveChanges();

					var tx = await _ethWriter.SendEth(r.Address, r.Amount.ToEther());
					r.Status = EthereumOperationStatus.BlockchainConfirm;
					r.Transaction = tx;
					r.RelUserFinHistory.RelEthTransactionId = tx;
					r.TimeNextCheck = DateTime.UtcNow.AddSeconds(60);

					ethAmount -= r.Amount.ToEther();

					try {
						// notification
						await EmailComposer.FromTemplate(await _templateProvider.GetEmailTemplate(EmailTemplate.ExchangeEthTransferred, r.RelUserFinHistory.RelUserActivity.Locale))
							.ReplaceBodyTag("REQUEST_ID", r.Id.ToString())
							.ReplaceBodyTag("ETHERSCAN_LINK", _appConfig.Services.Ethereum.EtherscanTxView + tx)
							.ReplaceBodyTag("DETAILS_SOURCE", r.RelUserFinHistory.SourceAmount + " GOLD")
							.ReplaceBodyTag("DETAILS_ESTIMATED", r.RelUserFinHistory.DestinationAmount + " ETH")
							.ReplaceBodyTag("DETAILS_ADDRESS", TextFormatter.MaskBlockchainAddress(r.Address))
							.Initiator(r.RelUserFinHistory.RelUserActivity)
							.Send(r.User.Email, r.User.UserName, _notificationQueue)
						;
					} catch{ }
				} catch (Exception e) {
					Logger.Error(e, $"Failed to process #{r.Id}");
				}
			}
		}
	}
}
