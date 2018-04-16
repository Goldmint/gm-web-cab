using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesPublisher : IAggregatedSafeRatesPublisher {

		private readonly SafeRatesPublisher _busPublisher;
		private readonly ILogger _logger;

		public BusSafeRatesPublisher(SafeRatesPublisher busPublisher, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_busPublisher = busPublisher;
		}

		public Task PublishRates(DateTime timestamp, SafeGoldRate goldRate, SafeCryptoRate cryptoRate) {

			_logger.Trace($"Publishing rates: stamp={ timestamp } / { goldRate } / { cryptoRate }");

			_busPublisher.PublishRates(new SafeRates() {

				Stamp = ((DateTimeOffset)timestamp).ToUnixTimeSeconds(),
				Gold = new Gold() {
					Usd = goldRate.Usd,
					
					IsSafeForBuy = goldRate.IsSafeForBuy,
					IsSafeForSell = goldRate.IsSafeForSell,
				},
				Crypto = new Crypto() {
					EthUsd = cryptoRate.EthUsd,
					IsSafeForBuy = cryptoRate.IsSafeForBuy,
					IsSafeForSell = cryptoRate.IsSafeForSell,
				}
			});

			return Task.CompletedTask;
		}
	}
}
