using System;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;

namespace Goldmint.QueueService.Workers {

	public interface IWorker {
		Task Loop(CancellationToken ct);
		Task Init(IServiceProvider services);
	}

	public abstract class BaseWorker : IWorker {

		private bool _launched;
		private TimeSpan _period;
		private TimeSpan _initialDelay;
		private bool _burstMode;
		private bool _selfStop;

		protected CancellationToken CancellationToken { get; private set; }
		protected ILogger Logger { get; private set; }

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

			CancellationToken = ct;
			if (_initialDelay > TimeSpan.Zero) {
				await Task.Delay(_initialDelay, CancellationToken);
			}

			Logger?.Verbose("Loop started");

			while (!IsCancelled()) {
				try {
					await OnUpdate();
				}
				catch (Exception e) {
					Logger?.Error(e, "loop failure");
					OnException(e);
				}
				try {
					OnPostUpdate();
				}
				catch (Exception e) {
					Logger?.Error(e, "loop failure (post)");
				}
				if (_burstMode) {
					break;
				}
				try {
					await Task.Delay(_period, CancellationToken);
				}
				catch { }
			}

			Logger?.Verbose("Loop stopped. Cleanup");
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

		// ---

		protected abstract Task OnInit(IServiceProvider services);
		protected abstract Task OnUpdate();
		
		protected virtual void OnCleanup() {}
		protected virtual void OnException(Exception e) {}
		protected virtual void OnPostUpdate() {}
		
	}
}
