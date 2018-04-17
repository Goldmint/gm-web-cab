using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class DefaultPublisher<T> : BasePublisher {

		public DefaultPublisher(Proto.Topic topic, Uri bindUri, LogFactory logFactory) : base(topic, 0xFFFF, logFactory) {
			PublisherSocket.Bind(bindUri.Scheme + "://*:" + bindUri.Port);
		}

		// ---

		private static byte[] Serialize(T data) {
			using (var ms = new MemoryStream()) {
				ProtoBuf.Serializer.Serialize(ms, data);
				ms.Position = 0;
				return ms.ToArray();
			}
		}

		public void PublishMessage(T message) {
			PublishMessage(Serialize(message));
		}
	}
}
