using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;

namespace Goldmint.QueueService.Workers.EthSender {

	public sealed class Confirmer : BaseWorker {

		private readonly int _rowsPerRound;
		private readonly int _ethConfirmations;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethReader;
		private IEthereumWriter _ethWriter;

		public Confirmer(int rowsPerRound, int ethConfirmations) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
			_ethConfirmations = Math.Max(2, ethConfirmations);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethReader = services.GetRequiredService<IEthereumReader>();
			_ethWriter = services.GetRequiredService<IEthereumWriter>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await (
				from r in _dbContext.EthSending
				where r.Status == EthereumOperationStatus.BlockchainConfirm && r.TimeNextCheck <= DateTime.UtcNow
				select r
			)
			.Include(_ => _.RelUserFinHistory)
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
						r.RelUserFinHistory.Status = txInfo.Status == EthTransactionStatus.Success? UserFinHistoryStatus.Completed: UserFinHistoryStatus.Failed;
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
