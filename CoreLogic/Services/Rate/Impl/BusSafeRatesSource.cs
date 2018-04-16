using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Threading;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesSource : IDisposable, IAggregatedSafeRatesSource {

		private readonly SafeRatesSubscriber _busSubscriber;
		private readonly ILogger _logger;
		private readonly ReaderWriterLockSlim _mutex;
		private readonly TimeSpan _freshDataTimeout;

		private DateTime _curRatesStamp;
		private SafeGoldRate _curSafeGoldRate;
		private SafeCryptoRate _curSafeCryptoRate;

		public BusSafeRatesSource(SafeRatesSubscriber busSubscriber, TimeSpan freshDataTimeout, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_mutex = new ReaderWriterLockSlim();
			_freshDataTimeout = freshDataTimeout;
			_busSubscriber = busSubscriber;
			_busSubscriber.SetCallback(OnNewRates);

			// unsafe
			_curRatesStamp = (DateTimeOffset.FromUnixTimeSeconds(0)).UtcDateTime;
			_curSafeGoldRate = new SafeGoldRate();
			_curSafeCryptoRate = new SafeCryptoRate();
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_mutex?.Dispose();
		}

		// ---

		private void OnNewRates(SafeRatesSubscriber safeRatesSubscriber, SafeRates safeRates) {
			_mutex.EnterWriteLock();
			try {
				_curRatesStamp = (DateTimeOffset.FromUnixTimeSeconds(safeRates.Stamp)).UtcDateTime;

				_curSafeGoldRate = new SafeGoldRate() {
					Usd = safeRates.Gold?.Usd ?? 0,
					IsSafeForBuy = safeRates.Gold?.IsSafeForBuy ?? false,
					IsSafeForSell = safeRates.Gold?.IsSafeForSell ?? false,
				};

				_curSafeCryptoRate = new SafeCryptoRate() {
					EthUsd = safeRates.Crypto?.EthUsd ?? 0,
					IsSafeForBuy = safeRates.Crypto?.IsSafeForBuy ?? false,
					IsSafeForSell = safeRates.Crypto?.IsSafeForSell ?? false,
				};

				_logger.Trace($"Received rates: stamp={ _curRatesStamp } / { _curSafeGoldRate } / { _curSafeCryptoRate }");
			}
			finally {
				_mutex.ExitWriteLock();
			}
		}

		public SafeCryptoRate GetCryptoRate() {
			_mutex.EnterReadLock();
			try {
				if (DateTime.UtcNow - _curRatesStamp <= _freshDataTimeout) {
					return _curSafeCryptoRate;
				}
				// unsafe
				return new SafeCryptoRate();
			}
			finally {
				_mutex.ExitReadLock();
			}
		}

		public SafeGoldRate GetGoldRate() {
			_mutex.EnterReadLock();
			try {
				if (DateTime.UtcNow - _curRatesStamp <= _freshDataTimeout) {
					return _curSafeGoldRate;
				}
				// unsafe
				return new SafeGoldRate();
			}
			finally {
				_mutex.ExitReadLock();
			}
		}


	}
}
