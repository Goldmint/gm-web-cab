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
		
		private readonly object _startStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;
		private TimeSpan _workerPeriod;
		private readonly ConcurrentQueue<CurrencyRate> _workerQueue;

		private readonly ReaderWriterLockSlim _mutexUpdate;
		private readonly Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public SafeRatesDispatcher(IAggregatedSafeRatesPublisher publisher, LogFactory logFactory) {
			_publisher = publisher;
			_logger = logFactory.GetLoggerFor(this);

			_startStopMonitor = new object();
			_workerCancellationTokenSource = new CancellationTokenSource();
			_mutexUpdate = new ReaderWriterLockSlim();
			_workerQueue = new ConcurrentQueue<CurrencyRate>();
			_workerPeriod = TimeSpan.FromSeconds(1);

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

		public void Run(TimeSpan period) {
			lock (_startStopMonitor) {
				if (_workerTask == null) {
					_workerPeriod = period;

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
		
		private void Worker() {
			var ctoken = _workerCancellationTokenSource.Token;

			while (!ctoken.IsCancellationRequested) {

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

						_publisher.PublishRates(_rates.Values.ToArray());
					}
					finally {
						_mutexUpdate.ExitWriteLock();
					}
				}
				
				Thread.Sleep(_workerPeriod);
			}

			_logger.Trace("Worker(): cancelled");
		}

		private void InterpolateFreshRate(CurrencyRate unsafeRate) {

			// TODO: interpolate
		}

		private SafeCurrencyRate ResolveSafety(CurrencyRate unsafeRate) {

			// TODO: resolve safety

			return new SafeCurrencyRate(
				canBuy: true,
				canSell: true,
				ttl: TimeSpan.FromSeconds(30),
				cur: unsafeRate.Currency,
				stamp: unsafeRate.Stamp,
				usd: unsafeRate.Usd
			);
		}
	}
}
