using System;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;

namespace Goldmint.QueueService.Workers {

	public sealed class BaseOptions {

		public TimeSpan Period { get; set; }
		public bool Once { get; set; }
		public CancellationToken CancellationToken { get; set; }

		public static BaseOptions RunOnce(CancellationToken ct) { return new BaseOptions { Once = true, CancellationToken = ct, }; }
		public static BaseOptions RunMinutely(CancellationToken ct) { return new BaseOptions { Period = TimeSpan.FromMinutes(1), CancellationToken = ct, }; }
		public static BaseOptions RunPeriod(CancellationToken ct, TimeSpan period) { return new BaseOptions { Period = period, CancellationToken = ct, }; }
	}

	public abstract class BaseWorker {

		private TimeSpan _period;
		private bool _once;
		private bool _selfStop;

		protected CancellationToken CancellationToken { get; private set; }
		protected ILogger Logger { get; private set; }

		protected BaseWorker(BaseOptions opts) {
			if (opts.Period.TotalSeconds < 1) {
				_period = TimeSpan.FromSeconds(1);
			} else {
				_period = opts.Period;
			}
			_once = opts.Once;
			if (opts.CancellationToken == null) {
				throw new ArgumentException("cancellation token is null");
			}
			CancellationToken = opts.CancellationToken;
		}

		protected abstract Task OnInit(IServiceProvider services);
		protected abstract Task OnCleanup();
		protected abstract Task OnUpdate();

		public async Task Init(IServiceProvider services) {
			Logger = services.GetLoggerFor(this.GetType());
			await OnInit(services);
		}

		public async Task Loop() {
			Logger?.Verbose("Loop started");

			while (!IsCancelled()) {
				try {
					await OnUpdate();
				}
				catch (Exception e) {
					Logger?.Error(e, "OnUpdate() failure");
				}
				if (_once) {
					break;
				}
				try {
					await Task.Delay(_period, CancellationToken);
				}
				catch (TaskCanceledException) { }
				catch (Exception e) {
					Logger?.Error(e, "Loop delay failure");
				}
			}

			Logger?.Verbose("Loop stopped. Cleanup");
			try {
				await OnCleanup();
			}
			catch (Exception e) {
				Logger?.Error(e, "OnCleanup() failure");
			}
		}

		// ---

		protected bool IsCancelled() {
			return CancellationToken.IsCancellationRequested || _selfStop;
		}

		protected void SelfStop() {
			_selfStop = true;
		}
	}
}
