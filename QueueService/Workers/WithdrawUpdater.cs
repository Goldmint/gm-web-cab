using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class WithdrawUpdater : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;

		public WithdrawUpdater(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			_dbContext.DetachEverything();

			// get withdrawal
			var nowTime = DateTime.UtcNow;
			var rows = await (
				from d in _dbContext.Withdraw
				where 
				(d.Status != Common.WithdrawStatus.Success && d.Status != Common.WithdrawStatus.Failed) && 
				d.TimeNextCheck <= nowTime
				select d
			)
				.Include(_ => _.RefFinancialHistory)
				.Include(_ => _.User)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				_dbContext.DetachEverything();

				await WithdrawQueue.ProcessWithdraw(_services, row);
			}
		}
	}
}
