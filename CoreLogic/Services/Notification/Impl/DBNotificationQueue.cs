using Goldmint.DAL;
using System;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;
using NatsSerializer = Goldmint.CoreLogic.Services.Bus.Nats.Serializer;
using NatsNotification = Goldmint.CoreLogic.Services.Bus.Nats.Notification;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public class DBNotificationQueue : IDisposable, INotificationQueue {

		private ApplicationDbContext _dbContext;
		private ILogger _logger;
		private NATS.Client.IConnection _natsConn;

		public DBNotificationQueue(ApplicationDbContext dbContext, NATS.Client.IConnection natsConn, ILogger logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
			_natsConn = natsConn;
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_natsConn.Close();
		}

		public async Task<bool> Enqueue(BaseNotification notification) {
			return await Enqueue(notification, DateTime.UtcNow);
		}

		public async Task<bool> Enqueue(BaseNotification notification, DateTime timeToSend) {

			var noti = new DAL.Models.Notification() {
				Type = notification.Type,
				JsonData = notification.SerializeContentToString(),
				TimeCreated = DateTime.UtcNow,
				TimeToSend = timeToSend,
			};

			_dbContext.Notification.Add(noti);
			await _dbContext.SaveChangesAsync();

			// nats request
			{
				var req = new NatsNotification.Enqueued.Request() {
					Id = noti.Id,
				};

				var msg = await _natsConn.RequestAsync(NatsNotification.Enqueued.Subject, NatsSerializer.Serialize(req), 2000);
				var rep = NatsSerializer.Deserialize<NatsNotification.Enqueued.Reply>(msg.Data);
				if (!rep.Success) {
					_logger.Error($"Failed to request notification sending via Nats: {rep.Error}");
					return false;
				}
			}
			return true;
		}
	}
}
