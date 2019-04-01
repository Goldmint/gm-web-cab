using Goldmint.CoreLogic.Finance;
using Goldmint.CoreLogic.Services.Bus.Telemetry;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.CreditCard {

	public sealed class VerificationProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		public VerificationProcessor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {

			_dbContext.DetachEverything();

			// get pending payments
			var nowTime = DateTime.UtcNow;
			var rows = await (
						from p in _dbContext.CreditCardPayment
						where
							p.Status == Common.CardPaymentStatus.Pending
							&& p.TimeNextCheck <= nowTime
							&& (p.Type == Common.CardPaymentType.Verification)
						select new { Id = p.Id, Type = p.Type }
					)
					.AsNoTracking()
					.Take(_rowsPerRound)
					.ToListAsync(CancellationToken)
				;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				if (IsCancelled()) return;

				_dbContext.DetachEverything();

				var res = await The1StPaymentsProcessing.ProcessVerificationPayment(_services, row.Id);
				if (res.Result == The1StPaymentsProcessing.ProcessVerificationPaymentResult.ResultEnum.Refunded) {
					++_statProcessed;
				}
				else if (res.Result != The1StPaymentsProcessing.ProcessVerificationPaymentResult.ResultEnum.NotFound) { // payment may be processed on related api call
					++_statFailed;
				}
			}
		}
	}
}
