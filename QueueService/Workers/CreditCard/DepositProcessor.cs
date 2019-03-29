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

	//public sealed class DepositProcessor : BaseWorker {

	//	private readonly int _rowsPerRound;

	//	private IServiceProvider _services;
	//	private ApplicationDbContext _dbContext;
	//	private CoreTelemetryAccumulator _coreTelemetryAccum;

	//	private long _statProcessed = 0;
	//	private long _statFailed = 0;

	//	public DepositProcessor(int rowsPerRound) {
	//		_rowsPerRound = Math.Max(1, rowsPerRound);
	//	}

	//	protected override Task OnInit(IServiceProvider services) {
	//		_services = services;
	//		_dbContext = services.GetRequiredService<ApplicationDbContext>();
	//		_coreTelemetryAccum = services.GetRequiredService<CoreTelemetryAccumulator>();

	//		return Task.CompletedTask;
	//	}

	//	protected override async Task OnUpdate() {

	//		_dbContext.DetachEverything();

	//		// get pending payments
	//		var nowTime = DateTime.UtcNow;
	//		var rows = await (
	//				from p in _dbContext.CreditCardPayment
	//				where
	//					p.Status == Common.CardPaymentStatus.Pending && 
	//					p.TimeNextCheck <= nowTime && 
	//					p.Type == Common.CardPaymentType.Deposit &&
	//					p.RelatedExchangeRequestId != null
	//				select new { Id = p.Id, RelatedExchangeRequestId = p.RelatedExchangeRequestId }
	//			)
	//			.AsNoTracking()
	//			.Take(_rowsPerRound)
	//			.ToListAsync(CancellationToken)
	//		;

	//		if (IsCancelled()) return;

	//		foreach (var row in rows) {

	//			if (IsCancelled()) return;

	//			_dbContext.DetachEverything();
				
	//			var res = await The1StPaymentsProcessing.ProcessDepositPayment(_services, row.Id);

	//			// charged
	//			if (res.Result == The1StPaymentsProcessing.ProcessDepositPaymentResult.ResultEnum.Charged) {

	//				var pdResult = await CoreLogic.Finance.GoldToken.OnCreditCardDepositCompleted(
	//					services: _services,
	//					requestId: row.RelatedExchangeRequestId ?? 0,
	//					paymentId: row.Id
	//				);

	//				Logger.Info(
	//					$"Request #{ row.RelatedExchangeRequestId } result is {pdResult.ToString()}"
	//				);

	//				++_statProcessed;
	//			}
	//			// failed to charge
	//			else if (res.Result == The1StPaymentsProcessing.ProcessDepositPaymentResult.ResultEnum.Failed) {

	//				var pdResult = await CoreLogic.Finance.GoldToken.OnCreditCardDepositCompleted(
	//					services: _services,
	//					requestId: row.RelatedExchangeRequestId ?? 0,
	//					paymentId: row.Id
	//				);

	//				Logger.Info(
	//					$"Request #{ row.RelatedExchangeRequestId } result is {pdResult.ToString()}"
	//				);

	//				++_statProcessed;
	//			}
	//			// unexpected
	//			else {
	//				++_statFailed;
	//			}
	//		}
	//	}

	//	protected override void OnPostUpdate() {

	//		// tele
	//		_coreTelemetryAccum.AccessData(tel => {
	//			tel.CreditCardDespoits.ProcessedSinceStartup = _statProcessed;
	//			tel.CreditCardDespoits.FailedSinceStartup = _statFailed;
	//			tel.CreditCardDespoits.Load = StatAverageLoad;
	//			tel.CreditCardDespoits.Exceptions = StatExceptionsCounter;
	//		});
	//	}
	//}
}
