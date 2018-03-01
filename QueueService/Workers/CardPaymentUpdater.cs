using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class CardPaymentUpdater : BaseWorker {

		private readonly int _rowsPerRound;

		private ApplicationDbContext _dbContext;
		private IServiceProvider _services;

		public CardPaymentUpdater(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			_dbContext.DetachEverything();

			// get pending payments
			var nowTime = DateTime.UtcNow;
			var rows = await (
				from p in _dbContext.CardPayment
				where 
				p.Status == Common.CardPaymentStatus.Pending 
				&& p.TimeNextCheck <= nowTime
				&& (p.Type == Common.CardPaymentType.Verification || p.Type == Common.CardPaymentType.Refund)
				select new { Id = p.Id, Type = p.Type }
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToListAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				if (row.Type == Common.CardPaymentType.Verification) {
					await CardPaymentQueue.ProcessVerificationPayment(_services, row.Id);
				}
				else if (row.Type == Common.CardPaymentType.Refund) {
					await CardPaymentQueue.ProcessRefundPayment(_services, row.Id);
				}

				// card-data-input payments might be processed here, but leave it for client redirects flow
			}
		}
	}
}
