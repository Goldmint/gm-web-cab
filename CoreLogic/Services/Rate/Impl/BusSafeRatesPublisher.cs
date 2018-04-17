using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesPublisher : IAggregatedSafeRatesPublisher {

		private readonly Bus.Publisher.DefaultPublisher<Bus.Proto.SafeRatesMessage> _busPublisher;
		private readonly ILogger _logger;

		public BusSafeRatesPublisher(Bus.Publisher.DefaultPublisher<Bus.Proto.SafeRatesMessage> busPublisher, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_busPublisher = busPublisher;
		}

		public Task PublishRates(SafeCurrencyRate[] rates) {

			_logger.Trace($"Publishing {rates.Length} rates");

			_busPublisher.PublishMessage(new Bus.Proto.SafeRatesMessage() {
				Rates = rates.Select(SafeCurrencyRate.BusSerialize).ToArray(),
			});

			return Task.CompletedTask;
		}
	}
}
