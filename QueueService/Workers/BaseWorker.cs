using Goldmint.Common;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public abstract class BaseWorker : IWorker {

		private bool _launched;
		private TimeSpan _period;
		private TimeSpan _initialDelay;
		private bool _burstMode;
		private bool _selfStop;

		protected CancellationToken CancellationToken { get; private set; }
		protected ILogger Logger { get; private set; }
		protected long LoopNumber { get; private set; }

		protected BaseWorker() {
			_period = TimeSpan.FromSeconds(10);
			_initialDelay = TimeSpan.Zero;
		}

		public async Task Init(IServiceProvider services) {
			if (_launched) throw new InvalidOperationException();

			Logger = services.GetLoggerFor(this.GetType());
			await OnInit(services);
		}

		public async Task Launch(CancellationToken ct) {
			if (_launched) throw new InvalidOperationException();
			_launched = true;

			CancellationToken = ct;
			if (_initialDelay > TimeSpan.Zero) {
				await Task.Delay(_initialDelay, CancellationToken);
			}

			Logger?.Trace("Loop started");

			var lastLoopStart = DateTime.UtcNow;
			while (!IsCancelled()) {
				try {

					lastLoopStart = DateTime.UtcNow;
					await Loop();
				}
				catch (Exception e) {
					Logger?.Error(e, "loop failure");
					OnException(e);
				}

				var loopTime = DateTime.UtcNow - lastLoopStart;

				var sleep = TimeSpan.Zero;
				if (loopTime < _period) {
					sleep = _period - loopTime;
				}

				if (sleep > TimeSpan.Zero) {
					try {
						await Task.Delay(sleep, CancellationToken);
					}
					catch { }
				}

				++LoopNumber;

				if (_burstMode) {
					break;
				}
			}
		}

		// ---

		public BaseWorker Period(TimeSpan period) {
			if (_launched) throw new InvalidOperationException();

			if (period.TotalSeconds < 1) period = TimeSpan.FromSeconds(1);
			_period = period;
			return this;
		}

		public BaseWorker BurstMode(bool burst = true) {
			if (_launched) throw new InvalidOperationException();
			_burstMode = burst;
			return this;
		}

		public BaseWorker InitialDelay(TimeSpan delay) {
			if (_launched) throw new InvalidOperationException();
			_initialDelay = delay;
			return this;
		}

		// ---

		protected bool IsCancelled() {
			return CancellationToken.IsCancellationRequested || _selfStop;
		}

		protected void SelfStop() {
			_selfStop = true;
		}

		protected TimeSpan GetPeriod() {
			return _period;
		}

		protected virtual void OnException(Exception e) {
		}

		protected abstract Task OnInit(IServiceProvider services);
		protected abstract Task Loop();
	}
}
