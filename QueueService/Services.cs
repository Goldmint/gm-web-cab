using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl;
using Goldmint.CoreLogic.Services.Google.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.Oplog.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Services.The1StPayments;
using Goldmint.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Blockchain.Sumus;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Impl;

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

		private static void SetupCommonServices(ServiceCollection services)
		{
			
			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(LogManager.LogFactory);

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
			services.AddScoped<IOplogProvider, DbOplogProvider>();

			// notifications
			services.AddSingleton<INotificationQueue, DBNotificationQueue>();

			// blockchain reader
			services.AddSingleton<IEthereumReader, EthereumReader>();
			services.AddSingleton<ISumusReader, SumusReader>();

			// rates helper
			services.AddSingleton<SafeRatesFiatAdapter>();

			// google sheets
			if (_appConfig.Services.GoogleSheets != null) {
				services.AddSingleton(new Sheets(_appConfig, LogManager.LogFactory));
			}
			
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
				var gmRateProvider = new GmRatesProvider(opts => {
					opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
					opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
				}, LogManager.LogFactory);
				services.AddSingleton<IGoldRateProvider>(gmRateProvider);
				services.AddSingleton<IEthRateProvider>(gmRateProvider);

				// custom central pub port
				var busChildPubCustomPort = Environment.GetEnvironmentVariable("ASPNETCORE_BUS_CNT_PORT");
				if (!string.IsNullOrWhiteSpace(busChildPubCustomPort)) {
					_appConfig.Bus.CentralPub.PubPort = int.Parse(busChildPubCustomPort);
				}

				// launch central pub
				_busCentralPublisher = new CoreLogic.Services.Bus.Publisher.CentralPublisher(_appConfig.Bus.CentralPub.PubPort, LogManager.LogFactory);
				services.AddSingleton(_busCentralPublisher);

				// rates dispatcher/local source
				_busSafeRatesPublisherWrapper = new BusSafeRatesPublisher(_busCentralPublisher, LogManager.LogFactory);
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
					_busSafeRatesPublisherWrapper, 
					_runtimeConfigHolder,
					LogManager.LogFactory, 
					opts => {
						opts.PublishPeriod = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.PubPeriodSec);
						opts.GoldTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.GoldValidForSec);
						opts.EthTtl = TimeSpan.FromSeconds(_appConfig.Bus.CentralPub.Rates.CryptoValidForSec);
					}
				);
				_busSafeRatesPublisherWrapper.SetCallback(ratesMsg => {
					_workerTelemetryAccumulator.AccessData(_ => _.RatesData = ratesMsg);
				});
				services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesPublisher>(_busSafeRatesPublisherWrapper);

				// telemetry accum-only
				_workerTelemetryAccumulator = new CoreLogic.Services.Bus.Telemetry.WorkerTelemetryAccumulator(
					null,
					TimeSpan.FromSeconds(1),
					LogManager.LogFactory
				);
				services.AddSingleton(_workerTelemetryAccumulator);
			}

			if (Mode.HasFlag(WorkingMode.Core)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();
				services.AddSingleton<ISumusWriter, SumusWriter>();

				// cc payment acquirer
				services.AddScoped<The1StPayments>(fac => {
					return new The1StPayments(opts => {
						opts.MerchantGuid = _appConfig.Services.The1StPayments.MerchantGuid;
						opts.ProcessingPassword = _appConfig.Services.The1StPayments.ProcessingPassword;
						opts.Gateway = _appConfig.Services.The1StPayments.Gateway;
						opts.RsInitStoreSms3D = _appConfig.Services.The1StPayments.RsInitStoreSms3D;
						opts.RsInitRecurrent3D = _appConfig.Services.The1StPayments.RsInitRecurrent3D;
						opts.RsInitStoreSms = _appConfig.Services.The1StPayments.RsInitStoreSms;
						opts.RsInitRecurrent = _appConfig.Services.The1StPayments.RsInitRecurrent;
						opts.RsInitStoreCrd = _appConfig.Services.The1StPayments.RsInitStoreCrd;
						opts.RsInitRecurrentCrd = _appConfig.Services.The1StPayments.RsInitRecurrentCrd;
						opts.RsInitStoreP2P = _appConfig.Services.The1StPayments.RsInitStoreP2P;
						opts.RsInitRecurrentP2P = _appConfig.Services.The1StPayments.RsInitRecurrentP2P;
					}, LogManager.LogFactory);
				});

				if (!Mode.HasFlag(WorkingMode.Worker)) {

					// subscribe to central pub
					_busCentralSubscriber = new CoreLogic.Services.Bus.Subscriber.CentralSubscriber(
						new [] { CoreLogic.Services.Bus.Proto.Topic.FiatRates, CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated },
						new Uri(_appConfig.Bus.CentralPub.Endpoint),
						LogManager.LogFactory
					);
					services.AddSingleton(_busCentralSubscriber);

					// rates from central pub
					_busSafeRatesSubscriberWrapper = new BusSafeRatesSource(_runtimeConfigHolder, LogManager.LogFactory);
					services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSubscriberWrapper);
					_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.FiatRates, (p, s) => {
						_busSafeRatesSubscriberWrapper.OnNewRates(p, s);
						_coreTelemetryAccumulator.AccessData(_ => _.RatesData = p as CoreLogic.Services.Bus.Proto.SafeRates.SafeRatesMessage);
					});

					// config update event
					_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated, (p, s) => {
						Task.Factory.StartNew(async () => {
							await _runtimeConfigHolder.Reload();
							var rcfg = _runtimeConfigHolder.Clone();
							_coreTelemetryAccumulator.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);
						});
					});
				}

				// custom child-pub port
				var busChildPubCustomPort = Environment.GetEnvironmentVariable("ASPNETCORE_BUS_CHD_PORT");
				if (!string.IsNullOrWhiteSpace(busChildPubCustomPort)) {
					_appConfig.Bus.ChildPub.PubPort = int.Parse(busChildPubCustomPort);
				}

				// launch child pub
				_busChildPublisher = new CoreLogic.Services.Bus.Publisher.ChildPublisher(_appConfig.Bus.ChildPub.PubPort, LogManager.LogFactory);
				services.AddSingleton(_busChildPublisher);

				// telemetry accum/pub
				_coreTelemetryAccumulator = new CoreLogic.Services.Bus.Telemetry.CoreTelemetryAccumulator(
					_busChildPublisher,
					TimeSpan.FromSeconds(_appConfig.Bus.ChildPub.PubTelemetryPeriodSec),
					LogManager.LogFactory
				);
				
				services.AddSingleton(_coreTelemetryAccumulator);
			}
		}

		private static void RunServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Run services");

			_runtimeConfigHolder.Reload().Wait();
			var rcfg = _runtimeConfigHolder.Clone();
			_workerTelemetryAccumulator?.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);
			_coreTelemetryAccumulator?.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);

			_busChildPublisher?.Run();
			_busCentralPublisher?.Run();
			_busCentralSubscriber?.Run();
			_safeAggregatedRatesDispatcher?.Run();
			// dont run worker's accumulator: _workerTelemetryAccumulator?.Run();
			_coreTelemetryAccumulator?.Run();
		}

		private static void StopServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			try {
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
			catch (Exception e) {
				logger.Error(e);
			}

			logger.Info("Services stopped");
		}
	}
}
