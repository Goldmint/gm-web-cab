using System;
using System.IO;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public sealed class SafeRatesSubscriber : BaseSubscriber {

		private Action<SafeRatesSubscriber, Proto.SafeRates> _cbk;

		public SafeRatesSubscriber(Uri connectUri, LogFactory logFactory) : base(1, logFactory) {

			SubscriberSocket.Subscribe(SafeRatesPublisher.Topic);
			SubscriberSocket.Connect(connectUri.ToString().TrimEnd('/'));
		}
		
		// ---

		public void SetCallback(Action<SafeRatesSubscriber, Proto.SafeRates> cbk) {
			_cbk = cbk;
		}

		protected override void OnMessage(string topic, DateTime stamp, Stream message) {
			var rates = Serializer.Deserialize<Proto.SafeRates>(message);
			_cbk?.Invoke(this, rates);
		}
	}
}
