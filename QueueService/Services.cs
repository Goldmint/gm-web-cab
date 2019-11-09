using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Price;
using Goldmint.CoreLogic.Services.Price.Impl;
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

		private static PriceDispatcher _priceDispatcher;
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
			var natsConnPool = new CoreLogic.Services.Bus.Impl.Bus(_appConfig, Log.Logger);
			services.AddSingleton<CoreLogic.Services.Bus.IBus>(natsConnPool);


			// prices
			{
				var gmPrices = new GMPriceProvider(
					opts => {
						opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
						opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
					},
					Log.Logger
				);

				_priceDispatcher = new PriceDispatcher(
					gmPrices, gmPrices, _runtimeConfigHolder, Log.Logger, 
					opts => {
						opts.PriceRequestPeriod = TimeSpan.FromSeconds(60);
						opts.PriceRequestTimeout = TimeSpan.FromSeconds(10);
					}
				);
				services.AddSingleton<IPriceSource>(_priceDispatcher);
			}


			// runtime config
			services.AddSingleton(_runtimeConfigHolder);
			services.AddSingleton<IRuntimeConfigLoader, DbRuntimeConfigLoader>();
			_configUpdater = new RuntimeConfigUpdater(natsConnPool, _runtimeConfigHolder, Log.Logger);

			services.AddSingleton<ITemplateProvider, TemplateProvider>();
		}

		private static void StartServices() {
			Log.Logger.Information("Run services");
			_priceDispatcher?.Start();
			_runtimeConfigHolder.Reload().Wait();
			_configUpdater?.Run();
		}

		private static void StopServices() {
			Log.Logger.Information("Stop services");

			try {
				_priceDispatcher?.Stop();
				_priceDispatcher?.Dispose();
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
