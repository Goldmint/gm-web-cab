using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public abstract class BaseSubscriber: IDisposable {

		protected readonly ILogger Logger;
		protected readonly SubscriberSocket SubscriberSocket;
		protected readonly NetMQPoller Poller;

		protected BaseSubscriber(int queueSize, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);

			SubscriberSocket = new SubscriberSocket();

			Poller = new NetMQPoller();
			Poller.Add(SubscriberSocket);

			SubscriberSocket.Options.SendHighWatermark = queueSize;
			SubscriberSocket.Options.ReceiveHighWatermark = queueSize;
			SubscriberSocket.Options.Endian = Endianness.Big;

			SubscriberSocket.Options.TcpKeepalive = true;
			SubscriberSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(60);
			SubscriberSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(60);

			SubscriberSocket.ReceiveReady += OnReceiveReady;
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			Poller?.Remove(SubscriberSocket);
			Poller?.Dispose();
			SubscriberSocket?.Dispose();
		}

		// ---

		public void Run() {
			Poller.RunAsync();
		}

		public void Stop() {
			Poller.StopAsync();
		}

		public bool IsRunning() {
			return Poller.IsRunning;
		}

		protected abstract void OnMessage(string topic, DateTime stamp, Stream message);

		private void OnReceiveReady(object sender, NetMQSocketEventArgs netMqSocketEventArgs) {
			if (netMqSocketEventArgs.Socket == SubscriberSocket) {

				var topic = SubscriberSocket.ReceiveFrameString();
				Logger.Debug($"Message received: { topic }");

				var stamp = SubscriberSocket.ReceiveFrameString();
				var message = SubscriberSocket.ReceiveFrameBytes();

				if (long.TryParse(stamp, out var stampUnix)) {
					using (var ms = new MemoryStream(message, false)) {
						ms.Position = 0;
						OnMessage(topic, DateTimeOffset.FromUnixTimeSeconds(stampUnix).UtcDateTime, ms);
					}
				}
				else {
					Logger.Error($"Failed to parse timestamp: { stamp }");
				}
			}
		}
	}
}
