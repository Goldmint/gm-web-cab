using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class SafeRatesDispatcher : IDisposable, IAggregatedSafeRatesSource, IAggregatedRatesDispatcher {

		private readonly IAggregatedSafeRatesPublisher _publisher;
		private readonly ILogger _logger;
		private readonly Options _opts;
		
		private readonly object _startStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;
		private readonly ConcurrentQueue<CurrencyRate> _workerQueue;

		private readonly ReaderWriterLockSlim _mutexUpdate;
		private readonly Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public SafeRatesDispatcher(IAggregatedSafeRatesPublisher publisher, LogFactory logFactory, Action<Options> opts) {

			_publisher = publisher;
			_logger = logFactory.GetLoggerFor(this);

			_opts = new Options() {
				PublishPeriod = TimeSpan.FromSeconds(1),
				GoldTtl = TimeSpan.FromSeconds(60),
				EthTtl = TimeSpan.FromSeconds(60),
			};
			opts?.Invoke(_opts);

			_startStopMonitor = new object();
			_workerCancellationTokenSource = new CancellationTokenSource();
			_mutexUpdate = new ReaderWriterLockSlim();
			_workerQueue = new ConcurrentQueue<CurrencyRate>();

			_rates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_logger.Trace("Disposing");

			Stop(true);
			_workerCancellationTokenSource?.Dispose();
			_workerTask?.Dispose();
			_mutexUpdate?.Dispose();
		}

		// ---

		public void OnProviderCurrencyRate(CurrencyRate rate) {
			_workerQueue.Enqueue(rate);
		}

		public SafeCurrencyRate GetRate(CurrencyRateType cur) {
			_mutexUpdate.EnterReadLock();
			try {
				if (_rates.TryGetValue(cur, out var ret)) {
					return ret;
				}
				return new SafeCurrencyRate(false, false, TimeSpan.Zero, cur, new DateTime(0, DateTimeKind.Utc), 0);
			}
			finally {
				_mutexUpdate.ExitReadLock();
			}
		}

		// ---

		public void Run(TimeSpan? period = null) {
			lock (_startStopMonitor) {
				if (_workerTask == null) {
					if (period != null) _opts.PublishPeriod = period.Value;

					_logger.Trace($"Run() period={ period }");
					_workerTask = Task.Factory.StartNew(Worker, TaskCreationOptions.LongRunning);
				}
			}
		}

		public void Stop(bool blocking = false) {
			lock (_startStopMonitor) {

				_logger.Trace("Stop(): send cancellation");
				_workerCancellationTokenSource.Cancel();

				if (blocking && _workerTask != null) {
					_logger.Trace("Stop(): wait for cancellation");
					_workerTask.Wait();
				}
			}
		}

		public async Task ForceUpdate() {
			await Update();
		}

		private async Task Worker() {
			var ctoken = _workerCancellationTokenSource.Token;

			while (!ctoken.IsCancellationRequested) {
				await Update();
				Thread.Sleep(_opts.PublishPeriod);
			}

			_logger.Trace("Worker(): cancelled");
		}

		private async Task Update() {

			var freshUnsafeRates = new Dictionary<CurrencyRateType, CurrencyRate>();

			// get most fresh item
			while (_workerQueue.TryDequeue(out var some)) {
				if (!freshUnsafeRates.TryGetValue(some.Currency, out var existing) || some.Stamp > existing.Stamp) {
					freshUnsafeRates[some.Currency] = some;
				}
			}

			var freshSafeRates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();

			// resolve safety
			foreach (var pair in freshUnsafeRates) {
				var unsafeRate = pair.Value;
				var curSafeRate = GetRate(unsafeRate.Currency);
				if (unsafeRate.Stamp > curSafeRate.Stamp) {
					InterpolateFreshRate(unsafeRate);
					freshSafeRates[unsafeRate.Currency] = ResolveSafety(unsafeRate);
				}
			}

			// update / publish
			if (freshSafeRates.Count > 0) {

				_mutexUpdate.EnterWriteLock();
				try {
					foreach (var pair in freshSafeRates) {
						_rates[pair.Key] = pair.Value;
					}
				}
				finally {
					_mutexUpdate.ExitWriteLock();
				}
			}

			// publish in any case
			if (_publisher != null) {
				await _publisher.PublishRates(_rates.Values.ToArray());
			}
		}

		private void InterpolateFreshRate(CurrencyRate unsafeRate) {

			// TODO: interpolate
		}

		private SafeCurrencyRate ResolveSafety(CurrencyRate unsafeRate) {

			// TODO: resolve safety

			TimeSpan ttl;
			switch (unsafeRate.Currency) {
				case CurrencyRateType.Gold: ttl = _opts.GoldTtl; break;
				case CurrencyRateType.Eth: ttl = _opts.EthTtl; break;
				default: throw new Exception($"Specify currency ttl { unsafeRate.Currency.ToString() }");
			}

			return new SafeCurrencyRate(
				canBuy: true,
				canSell: true,
				ttl: ttl,
				cur: unsafeRate.Currency,
				stamp: unsafeRate.Stamp,
				usd: unsafeRate.Usd
			);
		}

		// ---

		public sealed class Options {

			public TimeSpan PublishPeriod { get; set; }
			public TimeSpan GoldTtl { get; set; }
			public TimeSpan EthTtl { get; set; }
		}
	}
}
