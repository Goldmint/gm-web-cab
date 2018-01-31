using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public sealed class NullEmailSender : IEmailSender {

		public Task<bool> Send(EmailNotification notification) {
			return Task.FromResult(true);
		}
	}
}
