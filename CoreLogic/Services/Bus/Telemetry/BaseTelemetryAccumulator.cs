using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Telemetry {

	public abstract class BaseTelemetryAccumulator<T> : IDisposable where T:class {

		protected readonly ILogger Logger;
		private readonly object _runStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;
		private bool _running;

		private readonly ReaderWriterLockSlim _locker;
		private readonly ChildPublisher _publisher;
		private readonly Topic _topic;
		private readonly T _data;
		private readonly TimeSpan _pubPeriod;

		protected BaseTelemetryAccumulator(ChildPublisher publisher, Topic topic, T dataInstance, TimeSpan pubPeriod, LogFactory logFactory) {
			Logger = logFactory.GetLoggerFor(this);
			_publisher = publisher;
			_topic = topic;
			_data = dataInstance;
			_pubPeriod = pubPeriod;
			_locker = new ReaderWriterLockSlim();
			_runStopMonitor = new object();
			_workerCancellationTokenSource = new CancellationTokenSource();
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
			_locker?.Dispose();
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

		public void AccessData(Action<T> cbk) {
			try {
				_locker.EnterWriteLock();
				cbk?.Invoke(_data);
			}
			finally {
				_locker.ExitWriteLock();
			}
		}

		public T CloneData() {
			try {
				_locker.EnterReadLock();
				return _data.Copy();
			}
			finally {
				_locker.ExitReadLock();
			}
		}

		// ---

		private void Worker() {
			try {
				var ctoken = _workerCancellationTokenSource.Token;

				while (!ctoken.IsCancellationRequested) {

					if (_data != null) {
						try {
							_locker.EnterReadLock();

							_publisher.PublishMessage(_topic, _data);
							Logger.Trace("Telemetry published");
						}
						finally {
							_locker.ExitReadLock();
						}
					}

					var sleepUntil = DateTime.UtcNow.Add(_pubPeriod);
					while (DateTime.UtcNow < sleepUntil && !ctoken.IsCancellationRequested) {
						Thread.Sleep(250);
					}
				}
			}
			finally {

				Logger.Trace("Worker stopped");
				_running = false;
			}
		}
	}
}
