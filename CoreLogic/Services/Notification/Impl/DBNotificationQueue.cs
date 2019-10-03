using Goldmint.DAL;
using System;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;
using NatsSerializer = Goldmint.CoreLogic.Services.Bus.Serializer;
using NatsNotification = Goldmint.CoreLogic.Services.Bus.Models.Notification;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public class DBNotificationQueue : INotificationQueue {

		private ApplicationDbContext _dbContext;
		private Bus.IConnPool _bus;
		private ILogger _logger;

		public DBNotificationQueue(ApplicationDbContext dbContext, Bus.IConnPool bus, ILogger logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
			_bus = bus;
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
			try {
				using (var conn = await _bus.GetConnection()) {
					try {
						var req = new NatsNotification.Enqueued.Request() {
							Id = noti.Id,
						};

						var msg = await conn.RequestAsync(NatsNotification.Enqueued.Subject, NatsSerializer.Serialize(req), 2000);
						var rep = NatsSerializer.Deserialize<NatsNotification.Enqueued.Reply>(msg.Data);
						if (!rep.Success) {
							_logger.Error(new Exception(rep.Error), $"Failed to request notification sending via Nats");
							return false;
						}
					} finally {
						conn.Close();
					}
				}
			} catch (Exception e) {
				_logger.Error(e, $"Failed to get bus connection and send notification");
				return false;
			}
			return true;
		}
	}
}
