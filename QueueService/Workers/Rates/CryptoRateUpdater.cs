using Goldmint.CoreLogic.Services.Rate;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Rates {

	public sealed class CryptoRateUpdater : BaseWorker {

		private TimeSpan _requestTimeout;
		
		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthRateProvider _ethRateProvider;
		private IAggregatedRatesDispatcher _aggregatedRatesDispatcher;

		public CryptoRateUpdater() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethRateProvider = _services.GetRequiredService<IEthRateProvider>();
			_aggregatedRatesDispatcher = _services.GetRequiredService<IAggregatedRatesDispatcher>();

			_requestTimeout = TimeSpan.FromSeconds(10);

			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			try {
	
				var rate = await _ethRateProvider.RequestEthRate(_requestTimeout);
				Logger.Trace($"Current eth rate {rate}");

				_aggregatedRatesDispatcher.OnProviderCurrencyRate(rate);
			} catch (Exception e) {
				Logger.Error(e);
			}
		}
	}
}
