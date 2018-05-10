using Goldmint.QueueService.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goldmint.QueueService {

	public partial class Program {

		/// <summary>
		/// Launch workers
		/// </summary>
		private static List<Task> SetupWorkers(IServiceProvider services) {

			// general workers
			var workers = new List<IWorker>() {
#if DEBUG
				new DebugWorker().BurstMode()
#endif
			};

			// worker's workers
			if (Mode.HasFlag(WorkingMode.Worker)) {
				workers.AddRange(new List<IWorker>() { 

					// doesn't require ethereum at all
					new NotificationSender(_appConfig.Constants.Workers.Notifications.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.Notifications.PeriodSec)),
					new Workers.Rates.GoldRateUpdater().Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.GoldRateUpdater.PeriodSec)),
					new Workers.Rates.CryptoRateUpdater().Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.CryptoRateUpdater.PeriodSec)),
					new Workers.Telemetry.Aggregator().Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.TelemetryAggregator.PeriodSec)),
				});
			}

			// core workers
			if (Mode.HasFlag(WorkingMode.Core)) {
				workers.AddRange(new List<IWorker>() { 

					// does require ethereum (reader)
					new Workers.Ethereum.BuyRequestsHarvester(_appConfig.Constants.Workers.EthEventsHarvester.ItemsPerRound, _appConfig.Constants.Workers.EthEventsHarvester.EthConfirmations).Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.EthEventsHarvester.PeriodSec)),
					new Workers.Ethereum.SellRequestsHarvester(_appConfig.Constants.Workers.EthEventsHarvester.ItemsPerRound, _appConfig.Constants.Workers.EthEventsHarvester.EthConfirmations).Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.EthEventsHarvester.PeriodSec)),

					// does require ethereum (writer and reader)
					new Workers.Ethereum.EthereumOprationsProcessor(_appConfig.Constants.Workers.EthereumOperations.ItemsPerRound, _appConfig.Constants.Workers.EthereumOperations.EthConfirmations).Period(TimeSpan.FromSeconds(_appConfig.Constants.Workers.EthereumOperations.PeriodSec)),
				});
			}

			// init
			foreach (var w in workers) {
				var scopedServices = services.CreateScope().ServiceProvider;
				Task.Run(async () => await w.Init(scopedServices)).Wait();
			}

			// launch
			var workerTasks = new List<Task>();
			foreach (var w in workers) {
				workerTasks.Add(Task.Run(async () => await w.Loop(_shutdownToken.Token)));
			}

			return workerTasks;
		}
	}
}
