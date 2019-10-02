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

			var workers = new List<IWorker>() {
				
				// notifications
				new NotificationSender().BurstMode(),

				// currency price
				new Workers.Rates.GoldRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(30)),
				new Workers.Rates.CryptoRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(30)),

				// ethereum pool (contract)
				new Workers.EthPoolFreezer.EventHarvester(blocksPerRound: 50, confirmationsRequired: _appConfig.Services.Ethereum.ConfirmationsRequired).Period(TimeSpan.FromSeconds(90)),
				new Workers.EthPoolFreezer.SendTokenRequestor(rowsPerRound: 50).Period(TimeSpan.FromSeconds(60)),
				new Workers.EthPoolFreezer.SendTokenConfirmer().BurstMode(),

				// processes gold selling requests
				new Workers.Sell.RequestProcessor(rowsPerRound: 50).Period(TimeSpan.FromSeconds(60)),

				// eth sender
				new Workers.EthSender.Sender(rowsPerRound: 50).Period(TimeSpan.FromSeconds(60)),
				new Workers.EthSender.Confirmer(rowsPerRound: 50, ethConfirmations: _appConfig.Services.Ethereum.ConfirmationsRequired).Period(TimeSpan.FromSeconds(30)),

				// mint deposit wallets
				new Workers.SumusWallet.RefillListener().BurstMode(),
				new Workers.SumusWallet.TrackRequestor(rowsPerRound: 100).Period(TimeSpan.FromSeconds(60)),
			};

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
