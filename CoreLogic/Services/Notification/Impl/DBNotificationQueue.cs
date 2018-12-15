using Goldmint.Common;
using Goldmint.DAL;
using NLog;
using System;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public class DBNotificationQueue : INotificationQueue {

		private ApplicationDbContext _dbContext;
		private ILogger _logger;

		public DBNotificationQueue(ApplicationDbContext dbContext, LogFactory logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
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

			await _dbContext.Notification.AddAsync(noti);
			await _dbContext.SaveChangesAsync();
			return true;
		}
	}
}
