using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using Goldmint.Common.Extensions;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesSource : IDisposable, IAggregatedSafeRatesSource {

		private readonly NATS.Client.IConnection _natsConn;
		private readonly NATS.Client.IAsyncSubscription _natsSub;
		private readonly ILogger _logger;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

		private readonly ReaderWriterLockSlim _mutexRatesUpdate;
		private readonly Dictionary<CurrencyRateType, SafeCurrencyRate> _rates;

		public BusSafeRatesSource(NATS.Client.IConnection natsConn, RuntimeConfigHolder runtimeConfigHolder, LogFactory logFactory) {
			_natsConn = natsConn;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);
			_mutexRatesUpdate = new ReaderWriterLockSlim();
			_rates = new Dictionary<CurrencyRateType, SafeCurrencyRate>();
			
			_natsSub = _natsConn.SubscribeAsync(Bus.Nats.Rates.Updated.Subject);
			_natsSub.MessageHandler += OnNewRates;
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_natsConn.Close();
			_mutexRatesUpdate?.Dispose();
		}

		// ---

		public void Run() {
			_natsSub.Start();
		}

		public void Stop() {
			_natsSub.Unsubscribe();
		}

		private void OnNewRates(object sender, NATS.Client.MsgHandlerEventArgs args) {
			var ratesMessage = Bus.Nats.Serializer.Deserialize<Bus.Nats.Rates.Updated.Message>(args.Message.Data);
			
			_mutexRatesUpdate.EnterWriteLock();
			try {
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

				if (rcfg.Gold.AllowTradingOverall && _rates.TryGetValue(cur, out var ret)) {
					return ret;
				}
				return new SafeCurrencyRate(false, false, TimeSpan.Zero, cur, new DateTime(0, DateTimeKind.Utc), 0, 0);
			}
			finally {
				_mutexRatesUpdate.ExitReadLock();
			}
		}
	}
}
