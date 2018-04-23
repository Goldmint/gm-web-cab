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

		public BusSafeRatesSource(Bus.Subscriber.DefaultSubscriber<Bus.Proto.SafeRatesMessage> busSubscriber, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_mutexRatesUpdate = new ReaderWriterLockSlim();
			_rates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();

			busSubscriber.SetCallback(OnNewRates);
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_mutexRatesUpdate?.Dispose();
		}

		// ---

		public void OnNewRates(Bus.Subscriber.DefaultSubscriber<Bus.Proto.SafeRatesMessage> safeRatesSubscriber, Bus.Proto.SafeRatesMessage safeRatesMessage) {
			_mutexRatesUpdate.EnterWriteLock();
			try {
				_logger.Trace($"Received { safeRatesMessage.Rates.Length } rates");
				foreach (var v in safeRatesMessage.Rates) {
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
