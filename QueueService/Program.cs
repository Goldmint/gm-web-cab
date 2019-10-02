using Goldmint.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Serilog;

namespace Goldmint.QueueService {

	public partial class Program {

		private static IConfiguration _configuration;
		private static AppConfig _appConfig;
		private static IHostingEnvironment _environment;
		private static RuntimeConfigHolder _runtimeConfigHolder;

		private static CancellationTokenSource _shutdownToken;
		private static ManualResetEventSlim _shutdownCompletedEvent;

		// ---

		public static void Main(string[] args) {

			AppDomain.CurrentDomain.ProcessExit += onStop;
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				if (e.SpecialKey == ConsoleSpecialKey.ControlC) {
					e.Cancel = true;
					new Thread(delegate () {
						Environment.Exit(2);
					}).Start();
				}
			};

			_environment = new Microsoft.AspNetCore.Hosting.Internal.HostingEnvironment();
			_environment.EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			var curDir = Directory.GetCurrentDirectory();
			Console.OutputEncoding = Encoding.UTF8;

			_shutdownToken = new CancellationTokenSource();
			_shutdownCompletedEvent = new ManualResetEventSlim();

			// config
			try {
				var cfgDir = Environment.GetEnvironmentVariable("ASPNETCORE_CONFIGPATH");

				_configuration = new ConfigurationBuilder()
					.SetBasePath(cfgDir)
					.AddJsonFile("appsettings.json", optional: false)
					.AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", optional: false)
					.AddJsonFile($"appsettings.{_environment.EnvironmentName}.PK.json", optional: _environment.IsDevelopment())
					.Build()
				;

				_appConfig = new AppConfig();
				_configuration.Bind(_appConfig);
			}
			catch (Exception e) {
				throw new Exception("Failed to get app settings", e);
			}

			// serilog config
			var logConf = new LoggerConfiguration();
			{
				if (_environment.IsDevelopment()) {
					logConf.MinimumLevel.Verbose();
				}
				logConf.WriteTo.Console();
			}
			var logger = Log.Logger = logConf.CreateLogger();
			
			logger.Information("Starting");

			// runtime config
			_runtimeConfigHolder = new RuntimeConfigHolder(logger);

			// setup services
			var servicesCollection = new ServiceCollection();
			SetupCommonServices(servicesCollection);
			var services = servicesCollection.BuildServiceProvider();

			// config loader
			_runtimeConfigHolder.SetLoader(services.GetRequiredService<IRuntimeConfigLoader>());

			// setup ms logger
			// services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().AddNLog();

			// run services
			RunServices();

			// setup workers and wait
			Task.WaitAll(SetupWorkers(services).ToArray());

			// cleanup
			StopServices();
			Log.CloseAndFlush();

			_shutdownCompletedEvent.Set();
			logger.Information("Stopped");
		}

		private static void onStop(object sender, EventArgs e) {
			Console.WriteLine("Stop requested");
			_shutdownToken.Cancel();
			_shutdownCompletedEvent.Wait();
		}
	}
}
