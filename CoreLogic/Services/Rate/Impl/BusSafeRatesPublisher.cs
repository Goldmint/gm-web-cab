using System;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using NLog;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class BusSafeRatesPublisher : IAggregatedSafeRatesPublisher {

		private readonly Bus.Publisher.DefaultPublisher _busPublisher;
		private readonly ILogger _logger;
		private Action<Bus.Proto.SafeRates.SafeRatesMessage> _cbk;

		public BusSafeRatesPublisher(Bus.Publisher.DefaultPublisher busPublisher, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_busPublisher = busPublisher;
		}

		public Task PublishRates(SafeCurrencyRate[] rates) {

			_logger.Trace($"Publishing {rates.Length} rates");

			var msg = new Bus.Proto.SafeRates.SafeRatesMessage() {
				Rates = rates.Select(SafeCurrencyRate.BusSerialize).ToArray(),
			};

			_busPublisher.PublishMessage(Bus.Proto.Topic.FiatRates, msg);

			_cbk?.Invoke(msg);

			return Task.CompletedTask;
		}

		public void SetCallback(Action<Bus.Proto.SafeRates.SafeRatesMessage> cbk) {
			_cbk = cbk;
		}
	}
}
