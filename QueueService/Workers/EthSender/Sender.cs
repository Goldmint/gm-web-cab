using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Oplog;

namespace Goldmint.QueueService.Workers.EthSender {

	public sealed class Sender : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethReader;
		private IEthereumWriter _ethWriter;
		private IOplogProvider _oplog;

		public Sender(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethReader = services.GetRequiredService<IEthereumReader>();
			_ethWriter = services.GetRequiredService<IEthereumWriter>();
			_oplog = services.GetRequiredService<IOplogProvider>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await (
				from r in _dbContext.EthSending
				where r.Status == EthereumOperationStatus.Initial
				select r
			)
			.Include(_ => _.RelUserFinHistory)
			.AsTracking()
			.Take(_rowsPerRound)
			.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			var ethAmount = await _ethReader.GetEtherBalance(await _ethWriter.GetEthSender());

			foreach (var r in rows) {
				if (IsCancelled() || ethAmount < r.Amount.ToEther()) {
					return;
				}

				try {
					r.Status = EthereumOperationStatus.BlockchainInit;
					_dbContext.SaveChanges();
					try {
						await _oplog.Update(r.OplogId, UserOpLogStatus.Pending, $"Ether-sending transaction preparation");
					} catch {}

					var tx = await _ethWriter.SendEth(r.Address, r.Amount.ToEther());
					r.Status = EthereumOperationStatus.BlockchainConfirm;
					r.Transaction = tx;
					r.RelUserFinHistory.RelEthTransactionId = tx;
					r.TimeNextCheck = DateTime.UtcNow.AddSeconds(60);
					try {
						await _oplog.Update(r.OplogId, UserOpLogStatus.Pending, $"Ether-sending transaction {tx} posted");
					} catch {}

					ethAmount -= r.Amount.ToEther();
				} catch (Exception e) {
					Logger.Error(e, $"Failed to process #{r.Id}");
				}
			}
		}
	}
}
