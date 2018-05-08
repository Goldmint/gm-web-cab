using NetMQ.Sockets;
using NLog;
using ProtoBuf;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public sealed class DefaultSubscriber<T> : BaseSubscriber {

		private Action<DefaultSubscriber<T>, T> _cbk;

		public DefaultSubscriber(Proto.Topic[] topics, Uri connectUri, LogFactory logFactory) : base(topics, connectUri, 0xFFFF, logFactory) {
		}

		// ---

		private static T Deserialize(byte[] message) {
			using (var stream = new MemoryStream(message, false)) {
				return Serializer.Deserialize<T>(stream);
			}
		}
		
		public void SetCallback(Action<DefaultSubscriber<T>, T> cbk) {
			_cbk = cbk;
		}

		public bool ReceiveBlocking(out T result) {
			result = default(T);
			if (Receive(out var topic, out var stamp, out var message)) {
				result = Deserialize(message);
				return true;
			}
			return false;
		}

		protected override void OnNewMessage(string topic, DateTime stamp, byte[] message) {
			_cbk?.Invoke(this, Deserialize(message));
		}
	}
}
