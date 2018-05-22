using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Finance;
using Goldmint.CoreLogic.Services.Bus.Telemetry;

namespace Goldmint.QueueService.Workers.CreditCard {

	public sealed class PaymentProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private CoreTelemetryAccumulator _coreTelemetryAccum;

		private long _statProcessedVerifications = 0;
		private long _statFailedVerifications = 0;
		private long _statProcessedRefunds = 0;
		private long _statFailedRefunds = 0;

		public PaymentProcessor(int rowsPerRound) {
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
							&& (p.Type == Common.CardPaymentType.Verification || p.Type == Common.CardPaymentType.Refund)
						select new { Id = p.Id, Type = p.Type }
					)
					.AsNoTracking()
					.Take(_rowsPerRound)
					.ToListAsync(CancellationToken)
				;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				_dbContext.DetachEverything();

				if (row.Type == Common.CardPaymentType.Verification) {
					var res = await The1StPaymentsProcessing.ProcessVerificationPayment(_services, row.Id);
					if (res.Result == The1StPaymentsProcessing.ProcessVerificationPaymentResult.ResultEnum.Refunded) {
						++_statProcessedVerifications;
					}
					else {
						++_statFailedVerifications;
					}
				}
				else if (row.Type == Common.CardPaymentType.Refund) {
					var res = await The1StPaymentsProcessing.ProcessRefundPayment(_services, row.Id);
					if (res) {
						++_statProcessedRefunds;
					}
					else {
						++_statFailedRefunds;
					}
				}

				// card-data-input payments might be processed here, but leave it for client redirects flow
			}
		}

		protected override void OnPostUpdate() {

			// tele
			_coreTelemetryAccum.AccessData(tel => {
				tel.CreditCardVerificationPaymentProcessor.ProcessedSinceStartup = _statProcessedVerifications;
				tel.CreditCardVerificationPaymentProcessor.FailedSinceStartup = _statFailedVerifications;
				tel.CreditCardVerificationPaymentProcessor.Load = StatAverageLoad;
				tel.CreditCardVerificationPaymentProcessor.Exceptions = StatExceptionsCounter;
				tel.CreditCardRefundPaymentProcessor.ProcessedSinceStartup = _statProcessedRefunds;
				tel.CreditCardRefundPaymentProcessor.FailedSinceStartup = _statFailedRefunds;
				tel.CreditCardRefundPaymentProcessor.Load = StatAverageLoad;
				tel.CreditCardRefundPaymentProcessor.Exceptions = StatExceptionsCounter;
			});
		}
	}

}
