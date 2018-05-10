using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public abstract class BaseSubscriber: IDisposable {

		protected readonly Proto.Topic[] Topics;
		protected readonly string ConnectUri;
		protected readonly ILogger Logger;
		protected readonly SubscriberSocket SubscriberSocket;

		private readonly object _runStopMonitor;
		private readonly object _connectMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;

		private bool _running;
		private bool _connected;

		protected BaseSubscriber(Proto.Topic[] topics, Uri connect, int queueSize, LogFactory logFactory) {
			Topics = topics;
			ConnectUri = connect.ToString().TrimEnd('/');
			Logger = logFactory.GetLoggerFor(this);
			SubscriberSocket = new SubscriberSocket();
			foreach (var v in Topics) {
				SubscriberSocket.Subscribe(v.ToString());
			}

			_runStopMonitor = new object();
			_connectMonitor = new object();
			_workerCancellationTokenSource = new CancellationTokenSource();

			SubscriberSocket.Options.SendHighWatermark = queueSize;
			SubscriberSocket.Options.ReceiveHighWatermark = queueSize;
			SubscriberSocket.Options.Endian = Endianness.Big;

			// crashes in linux env (24 apr 2018)
			SubscriberSocket.Options.TcpKeepalive = false;
			// SubscriberSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(1);
			// SubscriberSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(3);

			SubscriberSocket.Options.ReconnectInterval = TimeSpan.FromSeconds(1);
			SubscriberSocket.Options.ReconnectIntervalMax = TimeSpan.FromSeconds(5);

			SubscriberSocket.ReceiveReady += OnSocketReceiveReady;
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			Logger.Trace("Disposing");
			
			Stop();
			Disconnect();

			_workerCancellationTokenSource?.Dispose();
			_workerTask?.Dispose();
			SubscriberSocket?.Dispose();
		}

		// ---

		public void Run() {
			lock (_runStopMonitor) {
				if (_workerTask == null) {
					Logger.Trace($"Run worker");
					_workerTask = Task.Factory.StartNew(Worker, TaskCreationOptions.LongRunning);
					_running = _workerTask != null;
				}
			}
		}

		public bool IsRunning() {
			return _running;
		}

		private void Stop() {
			lock (_runStopMonitor) {

				StopAsync();

				if (_workerTask != null) {
					Logger.Trace("Wait for worker");
					_workerTask.Wait();
				}
			}
		}

		public void StopAsync() {
			if (!_workerCancellationTokenSource.IsCancellationRequested) {
				Logger.Trace("Send stop event");
				_workerCancellationTokenSource.Cancel();
			}
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

				Logger.Trace($"Message received: { topic } / { stamp } / { message.Length }b");
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

		// ---

		private void Worker() {
			var ctoken = _workerCancellationTokenSource.Token;

			Connect();

			while (!ctoken.IsCancellationRequested) {

				if (!_connected) {
					Connect();
				}

				SubscriberSocket.Poll(TimeSpan.FromMilliseconds(200));
			}

			Disconnect();

			Logger.Trace("Worker stopped");
			_running = false;
		}

		public bool Connect() {
			lock (_connectMonitor) {
				if (!_connected) {
					SubscriberSocket.Connect(ConnectUri);
					_connected = true;
					return true;
				}

				return false;
			}
		}

		public bool Disconnect() {
			lock (_connectMonitor) {
				if (_connected) {
					SubscriberSocket.Disconnect(ConnectUri);
					_connected = false;
					return true;
				}

				return false;
			}
		}
	}
}
