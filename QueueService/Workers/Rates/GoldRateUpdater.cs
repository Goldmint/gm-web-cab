using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Rates {

	public sealed class GoldRateUpdater : BaseWorker {

		private TimeSpan _requestTimeout;
		
		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IGoldRateProvider _goldRateProvider;
		private IAggregatedRatesDispatcher _aggregatedRatesDispatcher;

		public GoldRateUpdater() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_goldRateProvider = _services.GetRequiredService<IGoldRateProvider>();
			_aggregatedRatesDispatcher = _services.GetRequiredService<IAggregatedRatesDispatcher>();

			_requestTimeout = TimeSpan.FromSeconds(10);

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

		// ---

		internal sealed class DbStorage {

		}
	}
}
