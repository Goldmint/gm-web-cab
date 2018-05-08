using Goldmint.Common;
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

		private readonly object _startStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;

		protected BasePublisher(Uri bindUri, int queueSize, LogFactory logFactory) {
			BindUri = bindUri.Scheme + "://*:" + bindUri.Port;
			Logger = logFactory.GetLoggerFor(this);
			PublisherSocket = new PublisherSocket();

			_startStopMonitor = new object();
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
			lock (_startStopMonitor) {
				if (_workerTask == null) {

					Logger.Trace($"Bind to " + BindUri);
					PublisherSocket.Bind(BindUri);

					Logger.Trace($"Run worker");
					_workerTask = Task.Factory.StartNew(Worker, TaskCreationOptions.LongRunning);
				}
			}
		}

		public bool IsRunning() {
			lock (_startStopMonitor) {
				return _workerTask != null;
			}
		}

		private void Stop() {
			lock (_startStopMonitor) {
				if (_workerTask != null) {
					Logger.Trace("Send stop event");
					_workerCancellationTokenSource.Cancel();

					Logger.Trace("Wait for worker");
					_workerTask.Wait();

					Logger.Trace("Unbind from " + BindUri);
					PublisherSocket.Unbind(BindUri);
				}
				_workerTask = null;
			}
		}

		// ---

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
			var ctoken = _workerCancellationTokenSource.Token;

			while (!ctoken.IsCancellationRequested) {
				// TODO: send ping
				Thread.Sleep(TimeSpan.FromMilliseconds(200));
			}

			Logger.Trace("Worker stopped");
		}
	}
}
