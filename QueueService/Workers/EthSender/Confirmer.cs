using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.EthSender {

	// Confirmer gets sent Ethereum transactions from DB and awaits tx confirmation
	public sealed class Confirmer : BaseWorker {

		private readonly int _rowsPerRound;
		private readonly int _ethConfirmations;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethReader;
		private IEthereumWriter _ethWriter;

		public Confirmer(int ethConfirmations, BaseOptions opts) : base(opts) {
			_rowsPerRound = 50;
			_ethConfirmations = Math.Max(2, ethConfirmations);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethReader = services.GetRequiredService<IEthereumReader>();
			_ethWriter = services.GetRequiredService<IEthereumWriter>();
			return Task.CompletedTask;
		}

		protected override Task OnCleanup() {
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await 
				(from r in _dbContext.EthSending where r.Status == EthereumOperationStatus.BlockchainConfirm && r.TimeNextCheck <= DateTime.UtcNow select r)
				.Include(_ => _.RelFinHistory)
				.AsTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			foreach (var r in rows) {
				if (IsCancelled()) {
					return;
				}

				try {
					var txInfo = await _ethReader.CheckTransaction(r.Transaction, _ethConfirmations);
					if (txInfo.Status == EthTransactionStatus.Success || txInfo.Status == EthTransactionStatus.Failed) {
						r.Status = txInfo.Status == EthTransactionStatus.Success? EthereumOperationStatus.Success: EthereumOperationStatus.Failed;
						if (r.RelFinHistory != null) {
							if (txInfo.Status == EthTransactionStatus.Success) {
								r.RelFinHistory.Status = UserFinHistoryStatus.Completed;
							} else {
								r.RelFinHistory.Status = UserFinHistoryStatus.Failed;
								r.RelFinHistory.Comment = "Ethereum transaction failed";
							}
						}
					} else {
						r.TimeNextCheck = DateTime.UtcNow.AddSeconds(60);
					}
					await _dbContext.SaveChangesAsync();
				} catch (Exception e) {
					Logger.Error(e, $"Failed to check #{r.Id}");
				}
			}
		}
	}
}
