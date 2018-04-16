using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Threading;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class SafeRatesDispatcher : IDisposable, IAggregatedSafeRatesSource, IAggregatedRatesDispatcher {

		private readonly IAggregatedSafeRatesPublisher _publisher;
		private readonly ILogger _logger;
		private readonly ReaderWriterLockSlim _mutexGold;
		private readonly ReaderWriterLockSlim _mutexCrypto;

		private SafeGoldRate _curSafeGoldRate;
		private SafeCryptoRate _curSafeCryptoRate;


		public SafeRatesDispatcher(IAggregatedSafeRatesPublisher publisher, LogFactory logFactory) {
			_publisher = publisher;
			_logger = logFactory.GetLoggerFor(this);

			_mutexGold = new ReaderWriterLockSlim();
			_mutexCrypto = new ReaderWriterLockSlim();

			_curSafeGoldRate = new SafeGoldRate();
			_curSafeCryptoRate = new SafeCryptoRate();
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_mutexGold?.Dispose();
			_mutexCrypto?.Dispose();
		}

		// ---

		public SafeCryptoRate GetCryptoRate() {
			_mutexCrypto.EnterReadLock();
			try {
				return _curSafeCryptoRate;
			}
			finally {
				_mutexCrypto.ExitReadLock();
			}
		}

		public SafeGoldRate GetGoldRate() {
			_mutexGold.EnterReadLock();
			try {
				return _curSafeGoldRate;
			}
			finally {
				_mutexGold.ExitReadLock();
			}
		}

		public void OnCryptoRate(CryptoRate rate, TimeSpan expectedPeriod) {

			// TODO: do not enter lock, use queue

			var stamp = DateTime.UtcNow;
			_mutexCrypto.EnterWriteLock();
			try {

				// TODO: checkCryptoRate();

				_curSafeCryptoRate = new SafeCryptoRate() {
					EthUsd = rate.EthUsd,
					IsSafeForBuy = true,
					IsSafeForSell = true,
				};
			}
			finally {
				_mutexCrypto.ExitWriteLock();
			}
		}

		public void OnGoldRate(GoldRate rate, TimeSpan expectedPeriod) {
			
			// TODO: dot not enter lock, use queue

			var stamp = DateTime.UtcNow;
			_mutexGold.EnterWriteLock();
			try {
				// TODO: checkGoldRate();

				_curSafeGoldRate = new SafeGoldRate() {
					Usd = rate.Usd,
					IsSafeForBuy = true,
					IsSafeForSell = true,
				};
			}
			finally {
				_mutexGold.ExitWriteLock();
			}
		}

		// TODO: process queue, check latest values (by timestamp), verify expected period, check safety, publish latest rates with timestamp
	}
}
