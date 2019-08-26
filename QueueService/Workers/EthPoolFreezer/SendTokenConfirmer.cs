using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using System.IO;
using Goldmint.CoreLogic.Services.Bus.Nats;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public class SendTokenConfirmer : BaseWorker {

		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;
		private ITemplateProvider _templateProvider;
		private INotificationQueue _notificationQueue;

		public SendTokenConfirmer() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			_templateProvider = services.GetRequiredService<ITemplateProvider>();
			_notificationQueue = services.GetRequiredService<INotificationQueue>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			using (var sub = _natsConn.SubscribeSync(Sumus.Sender.Sent.Subject)) {
				while (!IsCancelled()) {
					try {
						var msg = sub.NextMessage(1000);
						try {
							_dbContext.DetachEverything();

							// read msg
							var req = Serializer.Deserialize<Sumus.Sender.Sent.Request>(msg.Data);

							if (req.RequestID.StartsWith("buy-")) {
								// find request (buy)
								var id = long.Parse(req.RequestID.Substring(4));
								var row = await (
									from r in _dbContext.BuyGoldFiat where r.Id == id select r
								)
								.Include(_ => _.User)
								.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
								.AsTracking()
								.LastAsync();
								if (row == null) {
									throw new Exception($"Row #{id} (buy) not found");
								}

								// completed
								if (row.Status == SellGoldRequestStatus.Confirmed) {
									row.RelUserFinHistory.Comment += "; " + req.Transaction;
									row.Status = SellGoldRequestStatus.Success;
									row.TimeCompleted = DateTime.UtcNow;
									await _dbContext.SaveChangesAsync();

									try {
									// notification
									await EmailComposer.FromTemplate(await _templateProvider.GetEmailTemplate(EmailTemplate.ExchangeEthTransferred, Locale.En))
										.ReplaceBodyTag("REQUEST_ID", row.Id.ToString())
										.ReplaceBodyTag("SCANNER_LINK", "https://staging.goldmint.io/cabinet/#/scanner/tx/" + req.Transaction + "?network=testnet")
										.ReplaceBodyTag("DETAILS_SOURCE", TextFormatter.FormatAmount(row.FiatAmount, row.ExchangeCurrency))
										.ReplaceBodyTag("DETAILS_RATE", TextFormatter.FormatAmount(row.GoldRateCents, row.ExchangeCurrency) + " per GOLD")
										.ReplaceBodyTag("DETAILS_ESTIMATED", row.GoldAmount + " GOLD")
										.ReplaceBodyTag("DETAILS_ADDRESS", TextFormatter.MaskBlockchainAddress(row.Destination))
										.Initiator(row.RelUserFinHistory.RelUserActivity)
										.Send(row.User.Email, row.User.UserName, _notificationQueue)
									;
								} catch{ }
								
									_logger.Info($"Emission request #{row.Id} (buy) completed");
								}
							} else {
								// find request (freezer)
								var id = long.Parse(req.RequestID);
								var row = await (
									from r in _dbContext.PoolFreezeRequest
									where
										r.Id == id
									select r
								)
								.AsTracking()
								.LastAsync();
								if (row == null) {
									throw new Exception($"Row #{id} (freeze) not found");
								}

								// completed
								if (row.Status == EmissionRequestStatus.Requested) {
									row.SumTransaction = req.Transaction;
									row.Status = EmissionRequestStatus.Completed;
									row.TimeCompleted = DateTime.UtcNow;
									await _dbContext.SaveChangesAsync();
								
									_logger.Info($"Emission request #{row.Id} (freeze) completed");
								}
							}

							// reply
							var rep = new Sumus.Sender.Sent.Reply() { Success = true };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
						catch (Exception e) {
							_logger.Error(e, $"Failed to process message");
						}
					} catch{ }
				}
			}
		}
	}
}
