using Goldmint.CoreLogic.Services.Rate;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Rates {

	public sealed class GoldRateUpdater : BaseWorker {

		private readonly TimeSpan _requestTimeout;
		
		private IServiceProvider _services;
		private IGoldRateProvider _goldRateProvider;
		private IAggregatedRatesDispatcher _aggregatedRatesDispatcher;

		public GoldRateUpdater(TimeSpan requestTimeout) {
			_requestTimeout = requestTimeout;
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_goldRateProvider = _services.GetRequiredService<IGoldRateProvider>();
			_aggregatedRatesDispatcher = _services.GetRequiredService<IAggregatedRatesDispatcher>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			try {
				var rate = await _goldRateProvider.RequestGoldRate(_requestTimeout);
				Logger.Trace($"Current gold rate {rate}");
				_aggregatedRatesDispatcher.OnProviderCurrencyRate(rate);
			} catch (Exception e) {
				Logger.Error(e);
			}
		}
	}
}
