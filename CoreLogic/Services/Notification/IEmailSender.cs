using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Notification {

	public interface IEmailSender {

		Task<bool> Send(EmailNotification notification);
	}
}
