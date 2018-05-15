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
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.CoreLogic.Services.Ticket.Impl;
using Goldmint.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService {

	public partial class Program {

		private static CoreLogic.Services.Bus.Publisher.CentralPublisher _busCentralPublisher;
		private static CoreLogic.Services.Bus.Subscriber.CentralSubscriber _busCentralSubscriber;
		private static CoreLogic.Services.Bus.Publisher.ChildPublisher _busChildPublisher;
		
		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static BusSafeRatesPublisher _busSafeRatesPublisherWrapper;
		private static BusSafeRatesSource _busSafeRatesSubscriberWrapper;
		private static CoreLogic.Services.Bus.Telemetry.CoreTelemetryAccumulator _coreTelemetryAccumulator;
		private static CoreLogic.Services.Bus.Telemetry.WorkerTelemetryAccumulator _workerTelemetryAccumulator;

		private static void SetupCommonServices(ServiceCollection services) {
			
			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(_logFactory);

			// db
			services.AddDbContext<ApplicationDbContext>(opts => {
				opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
					myopts.UseRelationalNulls(true);
				});
			});

			// runtime config loader
			services.AddSingleton(_runtimeConfigHolder);
			services.AddSingleton<IRuntimeConfigLoader, DbRuntimeConfigLoader>();

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
				var gmRateProvider = new GmRatesProvider(_logFactory, opts => {
					opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
					opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
				});
				services.AddSingleton<IGoldRateProvider>(gmRateProvider);
				services.AddSingleton<IEthRateProvider>(gmRateProvider);
	
				// launch central pub
				_busCentralPublisher = new CoreLogic.Services.Bus.Publisher.CentralPublisher(new Uri(_appConfig.Bus.CentralPub.Endpoint), _logFactory);
				services.AddSingleton(_busCentralPublisher);

				// rates dispatcher/local source
				_busSafeRatesPublisherWrapper = new BusSafeRatesPublisher(_busCentralPublisher, _logFactory);
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
					_busSafeRatesPublisherWrapper, 
					_runtimeConfigHolder,
					_logFactory, 
					opts => {
						opts.PublishPeriod = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.PubPeriodSec);
						opts.GoldTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.GoldValidForSec);
						opts.EthTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.CryptoValidForSec);
					}
				);
				services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesPublisher>(_busSafeRatesPublisherWrapper);

				// telemetry accum-only
				_workerTelemetryAccumulator = new CoreLogic.Services.Bus.Telemetry.WorkerTelemetryAccumulator(
					null,
					TimeSpan.FromSeconds(1),
					_logFactory
				);
				services.AddSingleton(_workerTelemetryAccumulator);
			}

			if (Mode.HasFlag(WorkingMode.Core)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();

				if (!Mode.HasFlag(WorkingMode.Worker)) {

					// subscribe to central pub
					_busCentralSubscriber = new CoreLogic.Services.Bus.Subscriber.CentralSubscriber(
						new [] { CoreLogic.Services.Bus.Proto.Topic.FiatRates, CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated },
						new Uri(_appConfig.Bus.CentralPub.Endpoint),
						_logFactory
					);
					services.AddSingleton(_busCentralSubscriber);

					// rates from central pub
					_busSafeRatesSubscriberWrapper = new BusSafeRatesSource(_runtimeConfigHolder, _logFactory);
					services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSubscriberWrapper);
					_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.FiatRates, _busSafeRatesSubscriberWrapper.OnNewRates);

					// config update event
					_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated, (p, s) => {
						Task.Factory.StartNew(async () => { await _runtimeConfigHolder.Reload(); });
					});
				}

				// custom pub port
				var busPubCustomPort = Environment.GetEnvironmentVariable("ASPNETCORE_BUS_PUB_PORT");
				if (!string.IsNullOrWhiteSpace(busPubCustomPort)) {
					_appConfig.Bus.ChildPub.PubPort = int.Parse(busPubCustomPort);
				}

				// launch child pub
				_busChildPublisher = new CoreLogic.Services.Bus.Publisher.ChildPublisher(new Uri("tcp://localhost:" + _appConfig.Bus.ChildPub.PubPort), _logFactory);
				services.AddSingleton(_busChildPublisher);

				// telemetry accum/pub
				_coreTelemetryAccumulator = new CoreLogic.Services.Bus.Telemetry.CoreTelemetryAccumulator(
					_busChildPublisher,
					TimeSpan.FromSeconds(_appConfig.Bus.ChildPub.PubStatusPeriodSec),
					_logFactory
				);
				
				services.AddSingleton(_coreTelemetryAccumulator);
			}
		}

		private static void RunServices() {
			var logger = _logFactory.GetCurrentClassLogger();
			logger.Info("Run services");

			_runtimeConfigHolder.Reload().Wait();

			_busChildPublisher?.Run();
			_busCentralPublisher?.Run();
			_busCentralSubscriber?.Run();
			_safeAggregatedRatesDispatcher?.Run();
			_coreTelemetryAccumulator?.Run();
		}

		private static void StopServices() {
			var logger = _logFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			_safeAggregatedRatesDispatcher?.Stop(true);
			_busCentralPublisher?.StopAsync();
			_busCentralSubscriber?.StopAsync();
			_busChildPublisher?.StopAsync();

			_workerTelemetryAccumulator?.Dispose();
			_coreTelemetryAccumulator?.Dispose();
			_busSafeRatesSubscriberWrapper?.Dispose();
			_safeAggregatedRatesDispatcher?.Dispose();
			_busCentralPublisher?.Dispose();
			_busCentralSubscriber?.Dispose();
			_busChildPublisher?.Dispose();

			NetMQ.NetMQConfig.Cleanup(true);
		}
	}
}
