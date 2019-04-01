using Goldmint.QueueService.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

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
					new NotificationSender(_appConfig.Services.Workers.Notifications.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.Notifications.PeriodSec)),
					new Workers.Rates.GoldRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.GoldRateUpdater.PeriodSec)),
					new Workers.Rates.CryptoRateUpdater(TimeSpan.FromSeconds(_appConfig.Services.GMRatesProvider.RequestTimeoutSec)).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CryptoRateUpdater.PeriodSec)),
					new Workers.Bus.TelemetryAggregator().Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.TelemetryAggregator.PeriodSec)),
				});
			}

			// core workers
			if (Mode.HasFlag(WorkingMode.Core)) {
				workers.AddRange(new List<IWorker>() {

					// charges credit card
					new Workers.CreditCard.VerificationProcessor(
						_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound
					).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),
					// sends refunds back
					new Workers.CreditCard.RefundsProcessor(_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),
					//new Workers.CreditCard.DepositProcessor(_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),
					//new Workers.CreditCard.WithdrawProcessor(_appConfig.Services.Workers.CcPaymentProcessor.ItemsPerRound).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.CcPaymentProcessor.PeriodSec)),
					
					// harvests "frozen"-events (requests) from pool-freezer contract
					new Workers.EthPoolFreezer.EthEventHarvester(
						_appConfig.Services.Workers.EthEventsHarvester.ItemsPerRound,
						_appConfig.Services.Workers.EthEventsHarvester.EthConfirmations
					).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.EthEventsHarvester.PeriodSec)),
					// requires "frozen" stake to be emitted in sumus bc
					new Workers.EthPoolFreezer.EmissionRequestor(
						_appConfig.Services.Workers.TokenMigrationQueue.ItemsPerRound
					).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.TokenMigrationQueue.PeriodSec)),
					// confirms emission to complete harvested requests
					new Workers.EthPoolFreezer.EmissionConfirmer().BurstMode(),

					//// eth operations queue
					//new Workers.Ethereum.EthereumOprationsProcessor(
					//	_appConfig.Services.Workers.EthereumOperations.ItemsPerRound, 
					//	_appConfig.Services.Workers.EthereumOperations.EthConfirmations
					//).Period(TimeSpan.FromSeconds(_appConfig.Services.Workers.EthereumOperations.PeriodSec)),
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
