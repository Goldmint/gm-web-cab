using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Serilog;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class SafeRatesDispatcher : IDisposable, IAggregatedSafeRatesSource, IAggregatedRatesDispatcher {

		private readonly NATS.Client.IConnection _natsConn;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;
		private readonly ILogger _logger;
		private readonly Options _opts;
		
		private readonly object _startStopMonitor;
		private readonly CancellationTokenSource _workerCancellationTokenSource;
		private Task _workerTask;
		private readonly ConcurrentQueue<CurrencyRate> _workerQueue;

		private readonly ReaderWriterLockSlim _mutexUpdate;
		private readonly Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public SafeRatesDispatcher(Bus.IConnPool bus, RuntimeConfigHolder runtimeConfigHolder, ILogger logFactory, Action<Options> opts) {

			_natsConn = bus.GetConnection().Result;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);

			_opts = new Options() {
				AllowBuying = true,
				AllowSelling = true,
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
			_logger.Verbose("Disposing");

			Stop(true);
			_workerCancellationTokenSource?.Dispose();
			_workerTask?.Dispose();
			_mutexUpdate?.Dispose();

			_natsConn.Close();
			_natsConn.Dispose();
		}

		// ---

		public void OnProviderCurrencyRate(CurrencyRate rate) {
			_workerQueue.Enqueue(rate);
		}

		public SafeCurrencyRate GetRate(CurrencyRateType cur) {
			_mutexUpdate.EnterReadLock();
			try {
				var rcfg = _runtimeConfigHolder.Clone();
				if (rcfg.Gold.AllowTradingOverall && _rates.TryGetValue(cur, out var ret)) {
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

					_logger.Verbose($"Publishing period={ _opts.PublishPeriod }");
					_workerTask = Task.Factory.StartNew(Worker, TaskCreationOptions.LongRunning);
				}
			}
		}

		public void Stop(bool blocking = false) {
			lock (_startStopMonitor) {

				_logger.Verbose("Send cancellation");
				_workerCancellationTokenSource.Cancel();

				if (blocking && _workerTask != null) {
					_logger.Verbose("Wait for cancellation");
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
				try {
					await Update();
				}
				catch (Exception e) {
					_logger.Error(e, "Failed to update");
				}

				Thread.Sleep(_opts.PublishPeriod);
			}

			_logger.Verbose("Worker cancelled");
		}

		private Task Update() {

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
					//InterpolateFreshRate(unsafeRate);
					freshSafeRates[unsafeRate.Currency] = ResolveSafety(unsafeRate);
				}
			}

			// update
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

			// publish
			if (_natsConn != null && _rates.Count > 0) {
				var msg = new Bus.Models.Rates.Updated.Message() {
					Rates = _rates.Values.Select(SafeCurrencyRate.BusSerialize).ToArray(),
				};
				var bytes = Bus.Serializer.Serialize(msg);
				_natsConn.Publish(Bus.Models.Rates.Updated.Subject, bytes);
			}

			return Task.CompletedTask;
		}

		/*private void InterpolateFreshRate(CurrencyRate unsafeRate) {
		}*/

		private SafeCurrencyRate ResolveSafety(CurrencyRate unsafeRate) {

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

			public bool AllowBuying { get; set; }
			public bool AllowSelling { get; set; }
			public TimeSpan PublishPeriod { get; set; }
			public TimeSpan GoldTtl { get; set; }
			public TimeSpan EthTtl { get; set; }
		}
	}
}
