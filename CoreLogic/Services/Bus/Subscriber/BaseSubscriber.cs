using Goldmint.Common;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Bus.Proto;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public abstract class BaseSubscriber : IDisposable {

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
		private DateTime _lastHbTime;

		protected BaseSubscriber(Proto.Topic[] topics, Uri connect, int queueSize, LogFactory logFactory) {
			Topics = topics;
			ConnectUri = connect.ToString().TrimEnd('/');
			Logger = logFactory.GetLoggerFor(this);
			SubscriberSocket = new SubscriberSocket();

			SubscriberSocket.SubscribeToAnyTopic();
			/*foreach (var v in Topics) {
				SubscriberSocket.Subscribe(v.ToString());
			}
			SubscriberSocket.Subscribe(Proto.Topic.Hb.ToString());*/

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

			// SubscriberSocket.Options.ReconnectInterval = TimeSpan.FromSeconds(1);
			// SubscriberSocket.Options.ReconnectIntervalMax = TimeSpan.FromSeconds(5);

			SubscriberSocket.ReceiveReady += OnSocketReceiveReady;

			_lastHbTime = DateTime.UtcNow;
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			Logger.Trace("Disposing");

			Stop();

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
					while (_running) {
						Thread.Sleep(50);
					}
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

		protected abstract void OnNewMessage(Proto.Topic topic, DateTime stamp, byte[] message);

		protected bool Receive(out Proto.Topic topic, out DateTime stamp, out byte[] message) {
			topic = Topic.Unknown;
			stamp = DateTime.UtcNow;
			message = null;

			var parts = SubscriberSocket.ReceiveMultipartBytes(3);
			try {
				var tmptopic = Encoding.UTF8.GetString(parts[0]);
				var tmpstamp = BitConverter.ToInt64(parts[1], 0);
				var tmpmessage = parts[2];

				if (tmptopic != null && tmpmessage != null && tmpstamp > 0) {
					topic = Enum.Parse<Proto.Topic>(tmptopic, true);
					stamp = DateTimeOffset.FromUnixTimeSeconds(tmpstamp).UtcDateTime;
					message = tmpmessage;

					Logger.Trace($"RX: {topic} / {stamp} / {message.Length}b ({ConnectUri})");
					return true;
				}

				throw new Exception($"{ tmptopic } / { tmpstamp } / { tmpmessage }");
			}
			catch (Exception e) {
				Logger.Error(e, $"RX: corrupted message: { parts?.Count } frames, { parts?.Sum(_ => _.Length) } bytes / ({ ConnectUri })");
			}
			return false;
		}

		private void OnSocketReceiveReady(object sender, NetMQSocketEventArgs netMqSocketEventArgs) {
			if (netMqSocketEventArgs.Socket == SubscriberSocket && !_workerCancellationTokenSource.IsCancellationRequested) {
				try {
					if (Receive(out var topic, out var stamp, out var message)) {

						_lastHbTime = DateTime.UtcNow;

						// heartbeat
						if (topic == Proto.Topic.Hb) {
							// do nothing
						}
						else if (Topics.Contains(topic)) {
							OnNewMessage(topic, stamp, message);
						}
					}
				}
				catch (Exception e) {
					Logger.Error(e, $"Failed to read data from socket ({ ConnectUri })");
				}
			}
		}

		// ---

		private void Worker() {
			try {
				var ctoken = _workerCancellationTokenSource.Token;
				var nextConnTime = DateTime.UtcNow;

				Connect();

				while (!ctoken.IsCancellationRequested) {

					var now = DateTime.UtcNow;

					if (!_connected) {
						if (now >= nextConnTime && !Connect()) {
							Logger.Trace($"Reconnection failed. Retry in 2s ({ ConnectUri })");
							nextConnTime = DateTime.UtcNow.AddSeconds(2);
						}
					}
					else {
						if (now - _lastHbTime > TimeSpan.FromSeconds(4)) {
							Logger.Trace($"No messages for last 4s. Reconnection attempt ({ ConnectUri })");
							Disconnect();
						}
					}

					SubscriberSocket.Poll(TimeSpan.FromMilliseconds(200));
				}

				Disconnect();
			}
			finally {

				Logger.Trace("Worker stopped");
				_running = false;
			}
		}

		public bool Connect() {
			lock (_connectMonitor) {
				if (!_connected) {
					Logger.Trace($"Connection attempt ({ ConnectUri })");

					try {
						SubscriberSocket.Connect(ConnectUri);
						_lastHbTime = DateTime.UtcNow;
						_connected = true;
						return true;
					}
					catch { }
				}

				return false;
			}
		}

		public bool Disconnect() {
			lock (_connectMonitor) {
				if (_connected) {
					Logger.Trace($"Disconnection attempt ({ ConnectUri })");

					try {
						SubscriberSocket.Disconnect(ConnectUri);
					}
					catch { }

					_connected = false;
					return true;
				}

				return false;
			}
		}
	}
}
