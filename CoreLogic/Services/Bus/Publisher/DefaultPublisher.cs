using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public class DefaultPublisher : BasePublisher {

		public DefaultPublisher(int port, LogFactory logFactory) : base(port, 0xFFFF, logFactory) {
		}

		// ---

		private static byte[] Serialize<T>(T data) {
			using (var ms = new MemoryStream()) {
				ProtoBuf.Serializer.Serialize(ms, data);
				ms.Position = 0;
				return ms.ToArray();
			}
		}

		public void PublishMessage<T>(Proto.Topic topic, T message) {
			base.PublishMessage(topic, Serialize(message));
		}
	}
}
