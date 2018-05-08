using NLog;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public sealed class DefaultSubscriber : BaseSubscriber {

		private readonly Dictionary<Proto.Topic, Action<object, DefaultSubscriber>> _callbacks;

		public DefaultSubscriber(Proto.Topic[] topics, Uri connectUri, LogFactory logFactory) : base(topics, connectUri, 0xFFFF, logFactory) {
			_callbacks = new Dictionary<Proto.Topic, Action<object, DefaultSubscriber>>();
		}

		// ---

		protected override void OnNewMessage(string topic, DateTime stamp, byte[] message) {
			if (Enum.TryParse<Proto.Topic>(topic, true, out var top)) {
				switch (top) {
					case Proto.Topic.FiatRates:
						OnCallback(top, Deserialize<Proto.SafeRatesMessage>(message));
						break;

					// do nothing
					default:
						throw new NotImplementedException("Topic deserialization is not implemented for " + topic);
				}
			}
		}

		private void OnCallback(Proto.Topic topic, object payload) {
			if (_callbacks.TryGetValue(topic, out var cbk)) {
				cbk?.Invoke(payload, this);
			}
		}

		public void Callback(Proto.Topic topic, Action<object, DefaultSubscriber> cbk) {
			_callbacks[topic] = cbk;
		}

		private static T Deserialize<T>(byte[] message) {
			using (var stream = new MemoryStream(message, false)) {
				return Serializer.Deserialize<T>(stream);
			}
		}

		// ---

#if DEBUG

		public bool ReceiveBlocking<T>(out T result) {
			result = default(T);
			if (Receive(out var topic, out var stamp, out var message)) {
				result = Deserialize<T>(message);
				return true;
			}
			return false;
		}
#endif

	}
}
