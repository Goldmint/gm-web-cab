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
				new DebugWorker().Period(TimeSpan.FromSeconds(1))
#endif
			};

			// worker's workers
			if (Mode.HasFlag(WorkingMode.Worker)) {
				workers.AddRange(new List<IWorker>() { 

					// doesn't require ethereum at all
					new NotificationSender(rowsPerRound: 50).Period(TimeSpan.FromSeconds(10)),
					new Workers.Rates.GoldRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(30)),
					new Workers.Rates.CryptoRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(30)),
				});
			}

			// core workers
			if (Mode.HasFlag(WorkingMode.Core)) {
				workers.AddRange(new List<IWorker>() {

					// charges credit card
					new Workers.CreditCard.VerificationProcessor(rowsPerRound: 30).Period(TimeSpan.FromSeconds(30)),
					// sends refunds back
					new Workers.CreditCard.RefundsProcessor(rowsPerRound: 30).Period(TimeSpan.FromSeconds(30)),
					//new Workers.CreditCard.DepositProcessor(_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),
					//new Workers.CreditCard.WithdrawProcessor(_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),

					// harvests "frozen"-events (requests) from pool-freezer contract
					new Workers.EthPoolFreezer.FreezeEventHarvester(blocksPerRound: 10, confirmationsRequired: _appConfig.Services.Ethereum.ConfirmationsRequired).Period(TimeSpan.FromSeconds(60)),
					// requires "frozen" stake to be emitted in sumus bc
					new Workers.EthPoolFreezer.EmissionRequestor(rowsPerRound: 30).Period(TimeSpan.FromSeconds(30)),
					// confirms emission to complete harvested requests
					new Workers.EthPoolFreezer.EmissionConfirmer().BurstMode(),

					// processes gold selling requests
					new Workers.Sell.RequestProcessor(rowsPerRound: 50).Period(TimeSpan.FromSeconds(60)),

					// sends eth
					new Workers.EthSender.Sender(rowsPerRound: 50).Period(TimeSpan.FromSeconds(60)),
					// confirms eth-sendings
					new Workers.EthSender.Confirmer(rowsPerRound: 50, ethConfirmations: _appConfig.Services.Ethereum.ConfirmationsRequired).Period(TimeSpan.FromSeconds(30)),
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
