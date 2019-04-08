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

namespace Goldmint.QueueService {

	public partial class Program {

		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static BusSafeRatesSource _safeRatesSource;
		private static RuntimeConfigUpdater _configUpdater;

		private static void SetupCommonServices(ServiceCollection services) {

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

			// rates helper
			services.AddSingleton<SafeRatesFiatAdapter>();

			// google sheets
			if (_appConfig.Services.GoogleSheets != null) {
				services.AddSingleton(new Sheets(_appConfig, LogManager.LogFactory));
			}

			// nats factory
			var natsFactory = new NATS.Client.ConnectionFactory();

			// nats connection getter
			NATS.Client.IConnection natsConnGetter() {
				var opts = NATS.Client.ConnectionFactory.GetDefaultOptions();
				opts.Url = _appConfig.Bus.Nats.Endpoint;
				opts.AllowReconnect = true;
				return natsFactory.CreateConnection(opts);
			}
			services.AddScoped(_ => natsConnGetter());

			// runtime config updater
			_configUpdater = new RuntimeConfigUpdater(natsConnGetter(), natsConnGetter(), _runtimeConfigHolder, LogManager.LogFactory);

			// ---

			if (Mode.HasFlag(WorkingMode.Worker)) {

				// mail sender
				if (!_environment.IsDevelopment()) {
					services.AddSingleton<IEmailSender, MailGunSender>();
				}
				else {
					services.AddSingleton<IEmailSender, NullEmailSender>();
				}

				// currency rates providers
				var gmRateProvider = new GmRatesProvider(opts => {
					opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
					opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
				}, LogManager.LogFactory);
				services.AddSingleton<IGoldRateProvider>(gmRateProvider);
				services.AddSingleton<IEthRateProvider>(gmRateProvider);

				// rates dispatcher/publisher
				_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
					natsConnGetter(),
					_runtimeConfigHolder,
					LogManager.LogFactory,
					opts => {
						opts.PublishPeriod = TimeSpan.FromSeconds(3);
						opts.GoldTtl = TimeSpan.FromMinutes(60);
						opts.EthTtl = TimeSpan.FromMinutes(5);
					}
				);
				services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);
			}

			if (Mode.HasFlag(WorkingMode.Core)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();

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

				// rates source
				_safeRatesSource = new BusSafeRatesSource(natsConnGetter(), _runtimeConfigHolder, LogManager.LogFactory);
				services.AddSingleton<IAggregatedSafeRatesSource>(_safeRatesSource);
			}
		}

		private static void RunServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Run services");
			_runtimeConfigHolder.Reload().Wait();
			_safeAggregatedRatesDispatcher?.Run();
			_safeRatesSource?.Run();
			_configUpdater?.Run();
		}

		private static void StopServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			try {
				_safeAggregatedRatesDispatcher?.Stop(true);
				_safeAggregatedRatesDispatcher?.Dispose();
				_safeRatesSource?.Stop();
				_safeRatesSource?.Dispose();
				_configUpdater?.Stop();
				_configUpdater?.Dispose();
			}
			catch (Exception e) {
				logger.Error(e);
			}

			logger.Info("Services stopped");
		}
	}
}
