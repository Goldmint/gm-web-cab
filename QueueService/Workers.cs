using Goldmint.QueueService.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goldmint.QueueService {

	public partial class Program {

		// TODO: move/constants
		private static readonly int DefaultWorkerRowsPerRound = 100;
		private static readonly int DefaultCryptoHarvesterBlocksPerRound = 10;
		private static readonly int DefaultCryptoHarvesterConfirmationsRequired = 2;

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
					new NotificationSender(DefaultWorkerRowsPerRound).Period(TimeSpan.FromSeconds(3)),
					new GoldRateUpdater().Period(TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.Gold.PeriodSec)),
					new CryptoRateUpdater().Period(TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.Eth.PeriodSec)),
				});
			}

			// core workers
			if (Mode.HasFlag(WorkingMode.Core)) {
				workers.AddRange(new List<IWorker>() { 

					// does require ethereum (reader)
					new ContractBuyingEventHarvester(DefaultCryptoHarvesterBlocksPerRound, DefaultCryptoHarvesterConfirmationsRequired).Period(TimeSpan.FromSeconds(10)),
					new ContractSellingEventHarvester(DefaultCryptoHarvesterBlocksPerRound, DefaultCryptoHarvesterConfirmationsRequired).Period(TimeSpan.FromSeconds(10)),

					// does require ethereum (writer and reader)
					new TransferRequestProcessor(DefaultWorkerRowsPerRound).Period(TimeSpan.FromSeconds(10)),
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
				workerTasks.Add(Task.Run(async () => await w.Launch(_shutdownToken.Token)));
			}

			return workerTasks;
		}
	}
}
