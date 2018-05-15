using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Proto;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public abstract class BasePublisher: IDisposable {

		protected readonly string BindUri;
		protected readonly ILogger Logger;
		protected readonly PublisherSocket PublisherSocket;

		private readonly object _runStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;

		private bool _bound;
		private bool _running;
		private bool _enableHeartbeats;

		protected BasePublisher(Uri bindUri, int queueSize, LogFactory logFactory) {
			BindUri = bindUri.Scheme + "://*:" + bindUri.Port;
			Logger = logFactory.GetLoggerFor(this);
			PublisherSocket = new PublisherSocket();

			_enableHeartbeats = true;
			_runStopMonitor = new object();
			_workerCancellationTokenSource = new CancellationTokenSource();

			PublisherSocket.Options.SendHighWatermark = queueSize;
			PublisherSocket.Options.ReceiveHighWatermark = queueSize;
			PublisherSocket.Options.Endian = Endianness.Big;

			// crashes in linux env (24 apr 2018)
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

			Stop();

			_workerCancellationTokenSource?.Dispose();
			_workerTask?.Dispose();
			PublisherSocket?.Dispose();
		}

		// ---

		public void Run() {
			lock (_runStopMonitor) {
				if (_workerTask == null) {

					if (!_bound) {
						Logger.Trace($"Bind to " + BindUri);
						PublisherSocket.Bind(BindUri);
						_bound = true;
					}

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

				if (_bound) {
					Logger.Trace("Unbind from " + BindUri);
					PublisherSocket.Unbind(BindUri);
					_bound = false;
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

		public void EnableHeartbeats(bool enable) {
			_enableHeartbeats = enable;
		}

		protected void PublishMessage(Proto.Topic topic, byte[] message) {
			PublisherSocket
				// topic
				.SendMoreFrame(topic.ToString())
				// stamp
				.SendMoreFrame(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString())
				// body
				.SendFrame(message)
			;
			Logger.Trace($"Message sent: { topic.ToString() }");
		}

		// ---

		private void Worker() {
			try {
				var ctoken = _workerCancellationTokenSource.Token;

				var nextHbTime = DateTime.UtcNow;
				var hbPayload = new byte[0];

				while (!ctoken.IsCancellationRequested) {

					var now = DateTime.UtcNow;

					// send heartbeat
					if (_enableHeartbeats && now >= nextHbTime) {
						PublishMessage(Topic.Hb, hbPayload);
						nextHbTime = now.AddSeconds(2);
					}

					PublisherSocket.Poll(TimeSpan.FromMilliseconds(200));
				}
			}
			finally {
				Logger.Trace("Worker stopped");
				_running = false;
			}
		}
	}
}
