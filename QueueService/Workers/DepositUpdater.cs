using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class DepositUpdater : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;

		public DepositUpdater(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			_dbContext.DetachEverything();

			// get deposits
			var nowTime = DateTime.UtcNow;
			var rows = await (
				from d in _dbContext.Deposit
				where 
				(d.Status != Common.DepositStatus.Success && d.Status != Common.DepositStatus.Failed) && 
				d.TimeNextCheck <= nowTime
				select d
			)
				.Include(_ => _.FinancialHistory)
				.Include(_ => _.User)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {
				await DepositQueue.ProcessDeposit(_services, row);
			}
		}
	}
}
