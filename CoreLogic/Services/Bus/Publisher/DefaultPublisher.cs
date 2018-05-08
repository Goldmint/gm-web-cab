using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class DefaultPublisher<T> : BasePublisher {

		public DefaultPublisher(Uri bindUri, LogFactory logFactory) : base(bindUri, 0xFFFF, logFactory) {
		}

		// ---

		private static byte[] Serialize(T data) {
			using (var ms = new MemoryStream()) {
				ProtoBuf.Serializer.Serialize(ms, data);
				ms.Position = 0;
				return ms.ToArray();
			}
		}

		public void PublishMessage(Proto.Topic topic, T message) {
			PublishMessage(topic, Serialize(message));
		}
	}
}
