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
using Goldmint.Common;
using System.Collections.Generic;

namespace Goldmint.QueueService {

	public partial class Program {

		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static BusSafeRatesPublisher _busSafeRatesPublisherWrapper;
		private static BusSafeRatesSource _busSafeRatesSubscriberWrapper;

		//private static CoreLogic.Services.Bus.Publisher.DefaultPublisher _busSafeRatesPublisher;
		//private static CoreLogic.Services.Bus.Subscriber.DefaultSubscriber _busSafeRatesSubscriber;

		private static List<CoreLogic.Services.Bus.Publisher.DefaultPublisher> _busPublishers;
		private static List<CoreLogic.Services.Bus.Subscriber.DefaultSubscriber> _busSubscribers;

		private static void SetupCommonServices(ServiceCollection services) {

			_busPublishers = new List<CoreLogic.Services.Bus.Publisher.DefaultPublisher>();
			_busSubscribers = new List<CoreLogic.Services.Bus.Subscriber.DefaultSubscriber>();

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

				// rates publisher
				var workerPub = new CoreLogic.Services.Bus.Publisher.DefaultPublisher(
					new Uri(_appConfig.Bus.WorkerRates.PubUrl),
					_loggerFactory
				);
				_busSafeRatesPublisherWrapper = new BusSafeRatesPublisher(
					workerPub,
					_loggerFactory
				);
				workerPub.Run();
				_busPublishers.Add(workerPub);

				// rates dispatcher
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
					_busSafeRatesPublisherWrapper, 
					_loggerFactory, 
					opts => {
						opts.PublishPeriod = TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.PubPeriodSec);
						opts.GoldTtl = TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.Gold.ValidForSec);
						opts.EthTtl = TimeSpan.FromSeconds(_appConfig.Bus.WorkerRates.Eth.ValidForSec);
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

					var workerSub = new CoreLogic.Services.Bus.Subscriber.DefaultSubscriber(
						new [] { CoreLogic.Services.Bus.Proto.Topic.FiatRates },
						new Uri(_appConfig.Bus.WorkerRates.PubUrl),
						_loggerFactory
					);
					_busSafeRatesSubscriberWrapper = new BusSafeRatesSource(
						workerSub,
						_loggerFactory
					);
					workerSub.Run();
					_busSubscribers.Add(workerSub);

					services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSubscriberWrapper);
				}
			}
		}

		private static void StopCommonServices() {
			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("StopServices()");

			_safeAggregatedRatesDispatcher?.Dispose();

			if (_busPublishers != null)
				foreach (var v in _busPublishers) {
					v?.Dispose();
				}

			if (_busSubscribers != null)
				foreach (var v in _busSubscribers) {
					v?.Dispose();
				}

			NetMQ.NetMQConfig.Cleanup(true);
		}
	}
}
