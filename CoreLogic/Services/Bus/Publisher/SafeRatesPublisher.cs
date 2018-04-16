using Goldmint.CoreLogic.Services.Rate.Models;
using NetMQ.Sockets;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class SafeRatesPublisher : BasePublisher {

		public const string Topic = "FiatRates";

		public SafeRatesPublisher(Uri bindUri, LogFactory logFactory) : base(1, logFactory) {
			PublisherSocket.Bind(bindUri.Scheme + "://*:" + bindUri.Port);
		}

		protected override string PublisherTopic() {
			return Topic;
		}

		// ---

		public void PublishRates(Proto.SafeRates rates) {
			PublishMessage(rates);
		}
	}
}
