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
		private static void RunWorkers(IServiceProvider services) {
			var ct = _shutdownToken.Token;

			var workers = new BaseWorker[] {
				
				// notifications
				new NotificationSender(BaseOptions.RunOnce(ct)),

				// ethereum pool (contract)
				new Workers.EthPoolFreezer.EventHarvester(_appConfig.Services.Ethereum.ConfirmationsRequired, BaseOptions.RunPeriod(ct, TimeSpan.FromMinutes(2))),
				new Workers.EthPoolFreezer.EmissionRequestor(BaseOptions.RunMinutely(ct)),
				new Workers.EthPoolFreezer.EmissionConfirmer(BaseOptions.RunOnce(ct)),

				// processes gold buying requests
				new Workers.GoldBuy.Eth.Eth2GoldDepositer(BaseOptions.RunOnce(ct)),
				new Workers.GoldBuy.Withdraw.Confirmer(BaseOptions.RunOnce(ct)),

				// processes gold selling requests
				new Workers.GoldSell.Eth.RequestProcessor(BaseOptions.RunMinutely(ct)),

				// eth sender
				new Workers.EthSender.Sender(BaseOptions.RunMinutely(ct)),
				new Workers.EthSender.Confirmer(_appConfig.Services.Ethereum.ConfirmationsRequired, BaseOptions.RunMinutely(ct)),

				// mint deposit wallets
				new Workers.SumusWallet.RefillListener(BaseOptions.RunOnce(ct)),
				new Workers.SumusWallet.TrackRequestor(BaseOptions.RunMinutely(ct)),
			};

			foreach (var w in workers) {
				w.Init(services.CreateScope().ServiceProvider).Wait();
			}

			var tasks = new List<Task>();
			foreach (var w in workers) {
				var t = Task.Factory.StartNew(w.Loop, TaskCreationOptions.LongRunning);
				tasks.Add(t.Unwrap());
			}

			Task.WaitAll(tasks.ToArray());
		}
	}
}
