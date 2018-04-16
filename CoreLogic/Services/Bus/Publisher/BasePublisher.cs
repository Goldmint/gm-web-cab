using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public abstract class BasePublisher: IDisposable {

		protected readonly ILogger Logger;
		protected readonly PublisherSocket PublisherSocket;

		protected BasePublisher(int queueSize, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			PublisherSocket = new PublisherSocket();

			PublisherSocket.Options.SendHighWatermark = queueSize;
			PublisherSocket.Options.ReceiveHighWatermark = queueSize;
			PublisherSocket.Options.Endian = Endianness.Big;

			PublisherSocket.Options.TcpKeepalive = true;
			PublisherSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(60);
			PublisherSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(60);
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			PublisherSocket?.Dispose();
		}

		// ---

		protected abstract string PublisherTopic();

		protected void PublishMessage(byte[] message) {
			PublisherSocket
				// topic
				.SendMoreFrame(PublisherTopic())
				// stamp
				.SendMoreFrame(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString())
				// body
				.SendFrame(message)
			;
			Logger.Debug($"Message sent: { PublisherTopic() }");
		}

		protected void PublishMessage<T>(T message) {
			using (var ms = new MemoryStream()) {
				ProtoBuf.Serializer.Serialize(ms, message);
				PublishMessage(ms.ToArray());
			}
		}
	}
}
