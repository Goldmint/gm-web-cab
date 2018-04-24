using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public abstract class BasePublisher: IDisposable {

		protected readonly string BindUri;
		protected readonly Proto.Topic Topic;
		protected readonly ILogger Logger;
		protected readonly PublisherSocket PublisherSocket;

		protected BasePublisher(Uri bindUri, Proto.Topic topic, int queueSize, LogFactory logFactory) {
			BindUri = bindUri.Scheme + "://*:" + bindUri.Port;
			Topic = topic;
			Logger = logFactory.GetLoggerFor(this);

			PublisherSocket = new PublisherSocket();

			PublisherSocket.Options.SendHighWatermark = queueSize;
			PublisherSocket.Options.ReceiveHighWatermark = queueSize;
			PublisherSocket.Options.Endian = Endianness.Big;

			PublisherSocket.Options.TcpKeepalive = false;
			// PublisherSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(1);
			// PublisherSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(3);
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			Logger.Trace("Disposing");
			PublisherSocket?.Dispose();
		}

		// ---

		public void Bind() {
			PublisherSocket.Bind(BindUri);
		}

		public void Unbind() {
			PublisherSocket.Unbind(BindUri);
		}

		protected void PublishMessage(byte[] message) {
			PublisherSocket
				// topic
				.SendMoreFrame(Topic.ToString())
				// stamp
				.SendMoreFrame(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString())
				// body
				.SendFrame(message)
			;
			Logger.Trace($"Message sent: { Topic.ToString() }");
		}
	}
}
