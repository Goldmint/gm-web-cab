using Goldmint.Common;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.QueueService.Workers {

	public abstract class BaseWorker : IWorker {

		private bool _launched;
		private TimeSpan _period;
		private TimeSpan _initialDelay;
		private bool _burstMode;
		private bool _selfStop;

		protected CancellationToken CancellationToken { get; private set; }
		protected ILogger Logger { get; private set; }
		//protected long LoopNumber { get; private set; }

		//protected long StatExceptionsCounter { get; private set; }
		//protected int StatAverageLoad { get; private set; }

		protected BaseWorker() {
			_period = TimeSpan.FromSeconds(10);
			_initialDelay = TimeSpan.Zero;
		}

		public async Task Init(IServiceProvider services) {
			if (_launched) throw new InvalidOperationException();

			Logger = services.GetLoggerFor(this.GetType());
			await OnInit(services);
		}

		public async Task Loop(CancellationToken ct) {
			if (_launched) throw new InvalidOperationException();
			_launched = true;

			// var timingAvgPeriodSec = 60d;
			// var timingAvgMult = Math.Max(1, Math.Floor(timingAvgPeriodSec / (_period.TotalSeconds <= 0? timingAvgPeriodSec: _period.TotalSeconds)));
			// var timingAvgAccum = 0d;
			// var timingAvgAccumCounter = 0;

			CancellationToken = ct;
			if (_initialDelay > TimeSpan.Zero) {
				await Task.Delay(_initialDelay, CancellationToken);
			}

			Logger?.Trace("Loop started");

			// var lastLoopStart = DateTime.UtcNow;
			while (!IsCancelled()) {
				try {
					//lastLoopStart = DateTime.UtcNow;
					await OnUpdate();
				}
				catch (Exception e) {
					Logger?.Error(e, "loop failure");
					//++StatExceptionsCounter;
					OnException(e);
				}

				// load
				// var updateDuration = DateTime.UtcNow - lastLoopStart;
				// var timingLoad = updateDuration.TotalSeconds / (_period.TotalSeconds <= 0 ? updateDuration.TotalSeconds : _period.TotalSeconds);
				// timingAvgAccum = (timingAvgAccum * timingAvgAccumCounter + timingLoad) / (timingAvgAccumCounter + 1);
				// StatAverageLoad = (int)Math.Round(timingAvgAccum * 100);
				// if (timingAvgAccumCounter < timingAvgMult) ++timingAvgAccumCounter;

				try {
					OnPostUpdate();
				}
				catch (Exception e) {
					Logger?.Error(e, "loop failure (post)");
				}
				
				if (_burstMode) {
					break;
				}

				// time to sleep
				//var cycleDuration = DateTime.UtcNow - lastLoopStart;
				//var sleep = TimeSpan.Zero;
				//if (cycleDuration < _period) {
				//	sleep = _period - cycleDuration;
				//}
				//if (sleep > TimeSpan.Zero) {
				//	try {
				//		await Task.Delay(sleep, CancellationToken);
				//	}
				//	catch { }
				//}
				// ++LoopNumber;

				try {
					await Task.Delay(_period, CancellationToken);
				}
				catch { }
			}

			Logger?.Trace("Loop stopped. Cleanup");
			OnCleanup();
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

		protected virtual void OnCleanup() {
		}

		protected virtual void OnException(Exception e) {
		}

		protected virtual void OnPostUpdate() {
		}

		protected abstract Task OnInit(IServiceProvider services);
		protected abstract Task OnUpdate();
		
	}
}
