using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Notification {

	public interface INotificationQueue {

		Task<bool> Enqueue(BaseNotification notification);
		Task<bool> Enqueue(BaseNotification notification, DateTime timeToSend);
	}
}
