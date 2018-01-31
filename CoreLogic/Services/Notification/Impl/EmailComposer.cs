
using Goldmint.CoreLogic.Services.Localization;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Notification.Impl {

	public class EmailComposer {

		private readonly EmailNotification _email;

		public EmailComposer() {
			_email = new EmailNotification() {
			};
		}

		public static EmailComposer FromTemplate(string subject, string body) {
			var ret = new EmailComposer() {};
			ret.SetSubject(subject);
			ret.SetBody(body);
			return ret;
		}

		public static EmailComposer FromTemplate(EmailTemplate template) {
			var ret = new EmailComposer() {};
			ret.SetSubject(template.Subject);
			ret.SetBody(template.Body);
			return ret;
		}

		public EmailComposer ReplaceBodyTag(string tag, string value) {
			_email.Body = _email.Body?.Replace("{{" + tag + "}}", value);
			return this;
		}

		// ---

		public EmailComposer Initiator(string ip, string agent, DateTime time) {
			var timeFmt = time.ToString();
			ReplaceBodyTag("INITIATOR", $"<ul> <li>Date: {timeFmt} </li> <li>IP: {ip} </li> <li>Agent: {agent}</li> </ul>");
			return this;
		}

		public EmailComposer Username(string username) {
			ReplaceBodyTag("USERNAME", username);
			return this;
		}

		public EmailComposer Link(string link) {
			ReplaceBodyTag("LINK", link);
			return this;
		}

		public EmailComposer SetSubject(string subject) {
			_email.Subject = subject;
			return this;
		}

		public EmailComposer SetBody(string body) {
			_email.Body = body;
			return this;
		}

		public async Task<bool> Send(string address, INotificationQueue queue) {
			_email.Recipient = address;

			ReplaceBodyTag("HEAD", @"");

			return await queue.Enqueue(_email);
		}
	}
}
