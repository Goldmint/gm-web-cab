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

namespace Goldmint.QueueService {

	public partial class Program {

		private static CoreLogic.Services.Bus.Publisher.CentralPublisher _busCentralPublisher;
		private static CoreLogic.Services.Bus.Subscriber.CentralSubscriber _busCentralSubscriber;
		private static CoreLogic.Services.Bus.Publisher.ChildPublisher _busChildPublisher;

		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static BusSafeRatesPublisher _busSafeRatesPublisherWrapper;
		private static BusSafeRatesSource _busSafeRatesSubscriberWrapper;

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

			// rates helper
			services.AddSingleton<SafeRatesFiatAdapter>();
			
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
				var gmRateProvider = new GmRatesProvider(_loggerFactory, opts => {
					opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
					opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
				});
				services.AddSingleton<IGoldRateProvider>(gmRateProvider);
				services.AddSingleton<IEthRateProvider>(gmRateProvider);

				// launch central pub
				_busCentralPublisher = new CoreLogic.Services.Bus.Publisher.CentralPublisher(new Uri(_appConfig.Bus.CentralPub.Endpoint), _loggerFactory);
				_busCentralPublisher.Run();
				services.AddSingleton(_busCentralPublisher);

				// rates dispatcher/local source
				_busSafeRatesPublisherWrapper = new BusSafeRatesPublisher(_busCentralPublisher, _loggerFactory);
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
					_busSafeRatesPublisherWrapper, 
					_loggerFactory, 
					opts => {
						opts.PublishPeriod = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.PubPeriodSec);
						opts.GoldTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.GoldValidForSec);
						opts.EthTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.CryptoValidForSec);
					}
				);
				_safeAggregatedRatesDispatcher.Run();
				services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesPublisher>(_busSafeRatesPublisherWrapper);
			}

			if (Mode.HasFlag(WorkingMode.Core)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();

				// aggregated rates source (could be added in section above)
				if (services.Count(x => x.ServiceType == typeof(IAggregatedSafeRatesSource)) == 0) {

					// subscribe to central pub
					_busCentralSubscriber = new CoreLogic.Services.Bus.Subscriber.CentralSubscriber(
						new [] { CoreLogic.Services.Bus.Proto.Topic.FiatRates },
						new Uri(_appConfig.Bus.CentralPub.Endpoint),
						_loggerFactory
					);
					_busCentralSubscriber.Run();
					services.AddSingleton(_busCentralSubscriber);

					// rates from central pub
					_busSafeRatesSubscriberWrapper = new BusSafeRatesSource(_loggerFactory);
					_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.FiatRates, _busSafeRatesSubscriberWrapper.OnNewRates);
					services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSubscriberWrapper);
				}

				// launch child pub
				_busChildPublisher = new CoreLogic.Services.Bus.Publisher.ChildPublisher(new Uri("tcp://*:6669"), _loggerFactory);
				_busChildPublisher.Run();
				services.AddSingleton(_busChildPublisher);
			}
		}

		private static void StopCommonServices() {
			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("StopServices()");

			_busCentralPublisher?.Dispose();
			_busCentralSubscriber?.Dispose();
			_busChildPublisher?.Dispose();

			_busSafeRatesSubscriberWrapper?.Dispose();
			_safeAggregatedRatesDispatcher?.Dispose();
			
			NetMQ.NetMQConfig.Cleanup(true);
		}
	}
}
