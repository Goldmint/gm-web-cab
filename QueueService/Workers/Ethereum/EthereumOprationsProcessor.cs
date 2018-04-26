using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;

namespace Goldmint.QueueService.Workers.Ethereum {
	
	public sealed class EthereumOprationsProcessor : BaseWorker {

		private readonly int _rowsPerRound;
		private readonly int _ethConfirmations;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		
		public EthereumOprationsProcessor(int rowsPerRound, int ethConfirmations) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
			_ethConfirmations = Math.Max(2, ethConfirmations);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
				from r in _dbContext.EthereumOperation
				where 
				(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm) &&
				r.TimeNextCheck <= nowTime
				select new { Id = r.Id }
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				_dbContext.DetachEverything();

				await CoreLogic.Finance.EthereumContract.ExecuteOperation(_services, row.Id, _ethConfirmations);
			}
		}
	}
}
