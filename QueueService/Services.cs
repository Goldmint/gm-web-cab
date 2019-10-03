using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace Goldmint.QueueService {

	public partial class Program {

		private static SafeRatesDispatcher _safeAggregatedRatesDispatcher;
		private static RuntimeConfigUpdater _configUpdater;

		private static void SetupCommonServices(ServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton<ILogger>(Log.Logger);

			// db
			services.AddDbContext<ApplicationDbContext>(opts => {
				opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
					myopts.UseRelationalNulls(true);
				});
			});


			// notifications
			services.AddSingleton<INotificationQueue, DBNotificationQueue>();
				
			// mail sender
			if (!_environment.IsDevelopment()) {
				services.AddSingleton<IEmailSender, MailGunSender>();
			}
			else {
				services.AddSingleton<IEmailSender, NullEmailSender>();
			}
			

			// ethereum
			services.AddSingleton<IEthereumReader, EthereumReader>();
			services.AddSingleton<IEthereumWriter, EthereumWriter>();


			// nats
			var natsConnPool = new CoreLogic.Services.Bus.Impl.ConnPool(_appConfig, Log.Logger);
			services.AddSingleton<CoreLogic.Services.Bus.IConnPool>(natsConnPool);


			// runtime config
			services.AddSingleton(_runtimeConfigHolder);
			services.AddSingleton<IRuntimeConfigLoader, DbRuntimeConfigLoader>();
			_configUpdater = new RuntimeConfigUpdater(natsConnPool, _runtimeConfigHolder, Log.Logger);


			// rates
			services.AddSingleton<SafeRatesFiatAdapter>();

			// currency rates providers
			var gmRateProvider = new GmRatesProvider(opts => {
				opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
				opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
			}, Log.Logger);
			services.AddSingleton<IGoldRateProvider>(gmRateProvider);
			services.AddSingleton<IEthRateProvider>(gmRateProvider);

			// rates dispatcher/publisher
			_safeAggregatedRatesDispatcher = new SafeRatesDispatcher(
				natsConnPool,
				_runtimeConfigHolder,
				Log.Logger,
				opts => {
					opts.PublishPeriod = TimeSpan.FromSeconds(3);
					opts.GoldTtl = TimeSpan.FromMinutes(60);
					opts.EthTtl = TimeSpan.FromMinutes(5);
				}
			);
			services.AddSingleton<IAggregatedRatesDispatcher>(_safeAggregatedRatesDispatcher);
			services.AddSingleton<IAggregatedSafeRatesSource>(_safeAggregatedRatesDispatcher);


			services.AddSingleton<ITemplateProvider, TemplateProvider>();
		}

		private static void RunServices() {
			Log.Logger.Information("Run services");
			_runtimeConfigHolder.Reload().Wait();
			_safeAggregatedRatesDispatcher?.Run();
			_configUpdater?.Run();
		}

		private static void StopServices() {
			Log.Logger.Information("Stop services");

			try {
				_safeAggregatedRatesDispatcher?.Stop(true);
				_safeAggregatedRatesDispatcher?.Dispose();
				_configUpdater?.Stop();
				_configUpdater?.Dispose();
			}
			catch (Exception e) {
				Log.Logger.Error(e, "Failed to stop services");
			}

			Log.Logger.Information("Services stopped");
		}
	}
}
