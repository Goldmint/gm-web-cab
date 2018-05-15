using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesSource : IDisposable, IAggregatedSafeRatesSource {

		private readonly ILogger _logger;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

		private readonly ReaderWriterLockSlim _mutexRatesUpdate;
		private readonly Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public BusSafeRatesSource(RuntimeConfigHolder runtimeConfigHolder, LogFactory logFactory) {
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);
			_mutexRatesUpdate = new ReaderWriterLockSlim();
			_rates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_mutexRatesUpdate?.Dispose();
		}

		// ---

		public void OnNewRates(object payload, Bus.Subscriber.DefaultSubscriber self) {
			if (!(payload is Bus.Proto.SafeRates.SafeRatesMessage ratesMessage)) return;

			_mutexRatesUpdate.EnterWriteLock();
			try {
				_logger.Trace($"Received { ratesMessage.Rates.Length } rates");
				foreach (var v in ratesMessage.Rates) {
					var c = SafeCurrencyRate.BusDeserialize(v);
					if (!_rates.TryGetValue(c.Currency, out var existing) || c.Stamp > existing.Stamp) {
						_rates[c.Currency] = c;
					}
				}
			}
			finally {
				_mutexRatesUpdate.ExitWriteLock();
			}
		}

		public SafeCurrencyRate GetRate(CurrencyRateType cur) {
			_mutexRatesUpdate.EnterReadLock();
			try {
				var rcfg = _runtimeConfigHolder.Clone();

				if (rcfg.Gold.AllowTrading && _rates.TryGetValue(cur, out var ret)) {
					return ret;
				}
				return new SafeCurrencyRate(false, false, TimeSpan.Zero, cur, new DateTime(0, DateTimeKind.Utc), 0);
			}
			finally {
				_mutexRatesUpdate.ExitReadLock();
			}
		}
	}
}
