using Goldmint.CoreLogic.Services.Rate;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class CryptoRateUpdater : BaseWorker {

		private TimeSpan _requestTimeout;
		
		private IServiceProvider _services;
		private ICryptoCurrencyRateProvider _cryptoCurrencyRateProvider;
		private IAggregatedRatesDispatcher _aggregatedRatesDispatcher;

		public CryptoRateUpdater() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_cryptoCurrencyRateProvider = _services.GetRequiredService<ICryptoCurrencyRateProvider>();
			_aggregatedRatesDispatcher = _services.GetRequiredService<IAggregatedRatesDispatcher>();

			_requestTimeout = TimeSpan.FromSeconds(10);

			return Task.CompletedTask;
		}

		protected override async Task Loop() {
			try {
	
				var rate = await _cryptoCurrencyRateProvider.RequestCryptoRate(_requestTimeout);
				Logger.Trace($"Current crypto rate {rate}");

				_aggregatedRatesDispatcher.OnCryptoRate(rate, GetPeriod());
			} catch (Exception e) {
				Logger.Error(e);
			}
		}
	}
}
