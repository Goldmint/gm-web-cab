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
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;

namespace Goldmint.QueueService.Workers.CreditCard {

	public sealed class WithdrawProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private CoreTelemetryAccumulator _coreTelemetryAccum;
		private AppConfig _appConfig;
		private INotificationQueue _notificationQueue;
		private ITemplateProvider _templateProvider;


		private long _statProcessed = 0;
		private long _statFailed = 0;

		public WithdrawProcessor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_coreTelemetryAccum = services.GetRequiredService<CoreTelemetryAccumulator>();
			_appConfig = services.GetRequiredService<AppConfig>();
			_notificationQueue = services.GetRequiredService<INotificationQueue>();
			_templateProvider = services.GetRequiredService<ITemplateProvider>();

			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			
			_dbContext.DetachEverything();

			// get pending payments
			var nowTime = DateTime.UtcNow;
			var rows = await (
					from p in _dbContext.CreditCardPayment
					where
						p.Status == Common.CardPaymentStatus.Pending &&
						p.TimeNextCheck <= nowTime &&
						p.Type == Common.CardPaymentType.Withdraw &&
						p.RelatedExchangeRequestId != null
					select p
				)
				.Include(_ => _.CreditCard)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToListAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {

				if (IsCancelled()) return;

				_dbContext.DetachEverything();
				
				var res = await The1StPaymentsProcessing.ProcessWithdrawPayment(_services, row.Id);
				
				// success
				if (res.Result == The1StPaymentsProcessing.ProcessWithdrawalPaymentResult.ResultEnum.Withdrawn) {

					try {
						// get exchange request
						var request = await (
								from r in _dbContext.SellGoldRequest
								where
									r.Id == row.RelatedExchangeRequestId &&
									r.UserId == row.UserId
								select r
							)
							.Include(_ => _.RelUserFinHistory).ThenInclude(_ => _.RelUserActivity)
							.Include(_ => _.User)
							.FirstOrDefaultAsync()
						;

						// notification
						if (request?.RelUserFinHistory?.RelUserActivity != null) {
							await EmailComposer.FromTemplate(await _templateProvider.GetEmailTemplate(EmailTemplate.ExchangeFiatWithdrawal, request.RelUserFinHistory.RelUserActivity.Locale))
								.ReplaceBodyTag("REQUEST_ID", request.Id.ToString())
								.ReplaceBodyTag("DETAILS_SOURCE", request.RelUserFinHistory.SourceAmount + " GOLD")
								.ReplaceBodyTag("DETAILS_RATE", TextFormatter.FormatAmount(request.GoldRateCents) + "GOLD/" + request.ExchangeCurrency.ToString().ToUpper())
								.ReplaceBodyTag("DETAILS_ESTIMATED", TextFormatter.FormatAmount(row.AmountCents, row.Currency))
								.ReplaceBodyTag("DETAILS_ADDRESS", row.CreditCard.CardMask)
								.Initiator(request.RelUserFinHistory.RelUserActivity)
								.Send(request.User.Email, request.User.UserName, _notificationQueue)
							;
						}
					}
					catch (Exception e) {
						Logger.Error(e);
					}

					++_statProcessed;
				}
				// failed
				else if (res.Result == The1StPaymentsProcessing.ProcessWithdrawalPaymentResult.ResultEnum.Failed) {

					// TODO: notify support

					++_statFailed;
				}
				// unexpected
				else {
					++_statFailed;
				}
			}
		}

		protected override void OnPostUpdate() {

			// tele
			_coreTelemetryAccum.AccessData(tel => {
				tel.CreditCardWithdrawals.ProcessedSinceStartup = _statProcessed;
				tel.CreditCardWithdrawals.FailedSinceStartup = _statFailed;
				tel.CreditCardWithdrawals.Load = StatAverageLoad;
				tel.CreditCardWithdrawals.Exceptions = StatExceptionsCounter;
			});
		}
	}
}
