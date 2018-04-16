using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Blockchain.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.CoreLogic.Services.Ticket.Impl;
using Goldmint.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Goldmint.CoreLogic.Services.Bus.Subscriber;

namespace Goldmint.QueueService {

	public partial class Program {

		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static SafeRatesPublisher _busSafeRatesPublisher;
		private static BusSafeRatesPublisher _busSafeRatesPublisherWrapper;
		private static SafeRatesSubscriber _busSafeRatesSubscriber;
		private static BusSafeRatesSource _busSafeRatesSubscriberWrapper;

		/// <summary>
		/// DI services
		/// </summary>
		private static void SetupCommonServices(ServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(_loggerFactory);

			// db
			services.AddDbContext<ApplicationDbContext>(opts => {
				opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
					myopts.UseRelationalNulls(true);
				});
			});

			// mutex
			services.AddScoped<IMutexHolder, DBMutexHolder>(); 

			// templates
			services.AddSingleton<ITemplateProvider, TemplateProvider>();

			// ticket desk
			services.AddScoped<ITicketDesk, DBTicketDesk>();

			// notifications
			services.AddSingleton<INotificationQueue, DBNotificationQueue>();

			// blockchain reader
			services.AddSingleton<IEthereumReader, EthereumReader>();

			

			// ---

			if (Mode.HasFlag(WorkingMode.Worker)) {

				// mail sender
				if (!_environment.IsDevelopment()) {
					services.AddSingleton<IEmailSender, MailGunSender>();
				}
				else {
					services.AddSingleton<IEmailSender, NullEmailSender>();
				}

				// rate providers
				services.AddSingleton<IGoldRateProvider>(fac => new DebugRateProvider());
				services.AddSingleton<ICryptoCurrencyRateProvider>(fac => new DebugRateProvider());

				// rates publisher
				_busSafeRatesPublisher = new SafeRatesPublisher(
					new Uri(_appConfig.Bus.WorkerRates.Url),
					_loggerFactory
				);
				_busSafeRatesPublisherWrapper = new BusSafeRatesPublisher(
					_busSafeRatesPublisher,
					_loggerFactory
				);

				// rates dispatcher
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(_busSafeRatesPublisherWrapper, _loggerFactory);
				services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesPublisher>(_ => _busSafeRatesPublisherWrapper);
			}

			if (Mode.HasFlag(WorkingMode.Core)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();

				// aggregated rates source (could be added in section above)
				if (services.Count(x => x.ServiceType == typeof(IAggregatedSafeRatesSource)) == 0) {

					_busSafeRatesSubscriber = new SafeRatesSubscriber(
						new Uri(_appConfig.Bus.WorkerRates.Url),
						_loggerFactory
					);
					_busSafeRatesSubscriberWrapper = new BusSafeRatesSource(
						_busSafeRatesSubscriber,
						TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.ExpireTimeoutSec),
						_loggerFactory
					);
					services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSubscriberWrapper);
				}
			}
		}
	}
}
