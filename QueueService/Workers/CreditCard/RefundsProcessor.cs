using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Goldmint.CoreLogic.Finance;
using Goldmint.CoreLogic.Services.Bus.Telemetry;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.CreditCard {

	public sealed class RefundsProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private CoreTelemetryAccumulator _coreTelemetryAccum;

		private long _statProcessed = 0;
		private long _statFailed = 0;

		public RefundsProcessor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_coreTelemetryAccum = services.GetRequiredService<CoreTelemetryAccumulator>();

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
							&& (p.Type == Common.CardPaymentType.Refund)
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
				
				var res = await The1StPaymentsProcessing.ProcessRefundPayment(_services, row.Id);
				if (res) {
					++_statProcessed;
				}
				else {
					++_statFailed;
				}
			}
		}

		protected override void OnPostUpdate() {

			// tele
			_coreTelemetryAccum.AccessData(tel => {
				tel.CreditCardRefunds.ProcessedSinceStartup = _statProcessed;
				tel.CreditCardRefunds.FailedSinceStartup = _statFailed;
				tel.CreditCardRefunds.Load = StatAverageLoad;
				tel.CreditCardRefunds.Exceptions = StatExceptionsCounter;
			});
		}
	}
}
