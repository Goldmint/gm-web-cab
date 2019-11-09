using Goldmint.Common;
using Goldmint.CoreLogic.Services.Price.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Serilog;

namespace Goldmint.CoreLogic.Services.Price.Impl {

	public sealed class PriceDispatcher : IDisposable, IPriceSource {

		public sealed class Options {
			
			public TimeSpan PriceRequestPeriod { get; set; }
			public TimeSpan PriceRequestTimeout { get; set; }
		}

		private readonly RuntimeConfigHolder _runtimeConfigHolder;
		private readonly IGoldPriceProvider _goldPvd;
		private readonly IEthPriceProvider _ethPvd;
		private readonly ILogger _logger;
		private readonly Options _opts;
		
		private readonly object _startStopMonitor;
		private readonly CancellationTokenSource _workerCTS;
		private Task _workerTask;

		private readonly Dictionary<Common.CurrencyPrice, Models.CurrencyPrice> _rates;
		private readonly ReaderWriterLockSlim _ratesLock;

		public PriceDispatcher(IGoldPriceProvider goldPvd, IEthPriceProvider ethPvd, RuntimeConfigHolder runtimeConfigHolder, ILogger logFactory, Action<Options> opts) {
			_goldPvd = goldPvd;
			_ethPvd = ethPvd;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);

			_opts = new Options() {
				PriceRequestPeriod = TimeSpan.FromSeconds(60),
				PriceRequestTimeout = TimeSpan.FromSeconds(10),
			};
			opts?.Invoke(_opts);

			_startStopMonitor = new object();
			_workerCTS = new CancellationTokenSource();
			_ratesLock = new ReaderWriterLockSlim();

			_rates = new Dictionary<Common.CurrencyPrice, Models.CurrencyPrice>();
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			Stop(true);
			_workerCTS?.Dispose();
			_workerTask?.Dispose();
			_ratesLock?.Dispose();
		}

		public long? GetPriceInFiat(Common.CurrencyPrice currency, FiatCurrency fiatCurrency) {
			_ratesLock.EnterReadLock();
			try {
				long? cents = null;
				if (_rates.ContainsKey(currency)) {
					switch (fiatCurrency) {
						case FiatCurrency.Usd:
							cents = _rates[currency].Usd; 
							break;
						case FiatCurrency.Eur:
							cents = _rates[currency].Eur;
							break;
						default:
							throw new NotImplementedException("fiat currency not implemented");
					}
				}
				return cents;
			} finally {
				_ratesLock.ExitReadLock();
			}
		}

		private void UpdatePrices() {
			_ratesLock.EnterWriteLock();
			try {
				try {
					_rates[Common.CurrencyPrice.Gold] = _goldPvd.RequestGoldPrice(_opts.PriceRequestTimeout).Result;
				} catch (Exception e) {
					_logger.Error(e, "Failed to get GOLD price");
				}
				try {
					_rates[Common.CurrencyPrice.Eth] = _ethPvd.RequestEthPrice(_opts.PriceRequestTimeout).Result;
				} catch (Exception e) {
					_logger.Error(e, "Failed to get GOLD price");
				}
			} finally {
				_ratesLock.ExitWriteLock();
			}
		}

		// ---

		public void Start() {
			lock (_startStopMonitor) {
				if (_workerTask == null) {
					_workerTask = Task.Factory.StartNew(Worker, TaskCreationOptions.LongRunning);
				}
			}
		}

		public void Stop(bool blocking = false) {
			lock (_startStopMonitor) {
				_workerCTS.Cancel();
				if (blocking && _workerTask != null) {
					_workerTask.Wait();
				}
			}
		}

		private async Task Worker() {
			var ctoken = _workerCTS.Token;

			while (!ctoken.IsCancellationRequested) {
				try {
					UpdatePrices();
				}
				catch (Exception e) {
					_logger.Error(e, "Failed to update");
				}

				await Task.Delay(_opts.PriceRequestPeriod);
			}

			_logger.Verbose("Worker stopped");
		}
	}
}
