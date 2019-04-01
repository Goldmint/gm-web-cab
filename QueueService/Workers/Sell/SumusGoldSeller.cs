using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Bus.Telemetry;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Ethereum {

	public sealed class SumusGoldSeller : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;

		public SumusGoldSeller(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			//_dbContext.DetachEverything();
			//var nowTime = DateTime.UtcNow;

			//var rows = await (
			//	from r in _dbContext.SellGoldRequest
			//	where 
			//	(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm) &&
			//	r.TimeNextCheck <= nowTime
			//	select new { Id = r.Id }
			//)
			//	.AsNoTracking()
			//	.Take(_rowsPerRound)
			//	.ToArrayAsync(CancellationToken)
			//;

			//if (IsCancelled()) return;

			//foreach (var row in rows) {

			//	if (IsCancelled()) return;

			//	_dbContext.DetachEverything();

			//	if (await CoreLogic.Finance.EthereumContract.ExecuteOperation(_services, row.Id, _ethConfirmations)) {
			//		++_statProcessed;
			//	}
			//	else {
			//		++_statFailed;
			//	}
			//}
		}
	}
}
