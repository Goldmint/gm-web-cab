using Goldmint.DAL;
using System;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public class DBNotificationQueue : INotificationQueue {

		private ApplicationDbContext _dbContext;
		private Bus.IBus _bus;
		private ILogger _logger;

		public DBNotificationQueue(ApplicationDbContext dbContext, Bus.IBus bus, ILogger logFactory) {
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
				var request = new Bus.Models.Core.Sub.NotificationSendRequest() {
					ID = (ulong)noti.Id,
				};

				var reply = await _bus.Request(
					Bus.Models.Core.Sub.Subjects.NotificationSendRequest,
					request, Bus.Models.Core.Sub.NotificationSendResponse.Parser
				);

				if (!reply.Success) {
					_logger.Error(new Exception(reply.Error), $"Failed to request notification sending via Nats");
					return false;
				}
			} catch (Exception e) {
				_logger.Error(e, $"Failed to get bus connection and send notification");
				return false;
			}
			return true;
		}
	}
}
