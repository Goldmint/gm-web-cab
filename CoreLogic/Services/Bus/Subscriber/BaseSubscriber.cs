using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.IO;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public abstract class BaseSubscriber: IDisposable {

		// TODO: run custom task for reading, get rid of poller

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

			SubscriberSocket.ReceiveReady += OnSocketReceiveReady;
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

		// ---

		protected abstract void OnNewMessage(string topic, DateTime stamp, byte[] message);

		protected bool Receive(out string topic, out DateTime stamp, out byte[] message) {
			topic = null;
			stamp = DateTime.UtcNow;
			message = null;

			var hm = false;
			var tmptopic = SubscriberSocket.ReceiveFrameString();
			var tmpstamp = SubscriberSocket.ReceiveFrameString();
			var tmpmessage = SubscriberSocket.ReceiveFrameBytes(out hm);

			if (hm) {
				Stop();
				throw new Exception("There is some data after message. Invalid message format?");
			}

			if (tmptopic != null && tmpmessage != null && long.TryParse(tmpstamp, out var stampUnix)) {
				topic = tmptopic;
				stamp = DateTimeOffset.FromUnixTimeSeconds(stampUnix).UtcDateTime;
				message = tmpmessage;

				Logger.Debug($"Message received: { topic } / { stamp } / { message.Length }b");
				return true;
			}

			Logger.Error($"Message received: { tmptopic } / { tmpstamp } / { tmpmessage?.Length }b");
			return false;
		}

		private void OnSocketReceiveReady(object sender, NetMQSocketEventArgs netMqSocketEventArgs) {
			if (netMqSocketEventArgs.Socket == SubscriberSocket) {
				if (Receive(out var topic, out var stamp, out var message)) {
					OnNewMessage(topic, stamp, message);
				}
			}
		}
	}
}
