using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesSource : IDisposable, IAggregatedSafeRatesSource {

		private readonly ILogger _logger;

		private readonly ReaderWriterLockSlim _mutexRatesUpdate;
		private Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public BusSafeRatesSource(Bus.Subscriber.DefaultSubscriber busSubscriber, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_mutexRatesUpdate = new ReaderWriterLockSlim();
			_rates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();

			busSubscriber.Callback(Bus.Proto.Topic.FiatRates, OnNewRates);
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_mutexRatesUpdate?.Dispose();
		}

		// ---

		public void OnNewRates(object payload, Bus.Subscriber.DefaultSubscriber self) {
			if (!(payload is Bus.Proto.SafeRatesMessage ratesMessage)) return;

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
				if (_rates.TryGetValue(cur, out var ret)) {
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
