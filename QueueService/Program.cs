using Goldmint.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog.Web;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Finance;

namespace Goldmint.QueueService {

	public partial class Program {

		[Flags]
		public enum WorkingMode : int {
			Worker = 1,
			Core = 2,
		}

		public static WorkingMode Mode { get; private set; }

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

			// resolve working mode
			var workingModeRaw = Environment.GetEnvironmentVariable("ASPNETCORE_MODE") ?? "";
			if (workingModeRaw.Contains("worker")) Mode |= WorkingMode.Worker;
			if (workingModeRaw.Contains("core")) Mode |= WorkingMode.Core;
			if (Mode == 0) {
				throw new Exception("Mode must be specified in args");
			}

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

			// nlog config/factory
			LogManager.LoadConfiguration(Path.Combine(curDir, $"nlog.{_environment.EnvironmentName}.config"));
#if DEBUG
			LogManager.LogFactory.KeepVariablesOnReload = true;
			LogManager.LogFactory.Configuration.Variables["logDirectory"] = "/log/qs/core/";
			LogManager.LogFactory.Configuration.Reload();
			LogManager.LogFactory.ReconfigExistingLoggers();
#endif

			// this class logger
			var logger = LogManager.LogFactory.GetLogger(typeof(Program).FullName);
			logger.Info($"Launched ({ Mode.ToString() })");

			// runtime config
			_runtimeConfigHolder = new RuntimeConfigHolder(LogManager.LogFactory);

			// custom db connection
			var dbCustomConnection = Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION");
			if (!string.IsNullOrWhiteSpace(dbCustomConnection)) {
				_appConfig.ConnectionStrings.Default = dbCustomConnection;
				logger.Info($"Using custom db connection: {dbCustomConnection}");
			}

			// setup services
			var servicesCollection = new ServiceCollection();
			SetupCommonServices(servicesCollection);
			var services = servicesCollection.BuildServiceProvider();

			// config loader
			_runtimeConfigHolder.SetLoader(services.GetRequiredService<IRuntimeConfigLoader>());

			// setup ms logger
			services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().AddNLog();

			// run services
			RunServices();

			// setup workers and wait
			Task.WaitAll(SetupWorkers(services).ToArray());

			// cleanup
			StopServices();
			LogManager.Shutdown();

			_shutdownCompletedEvent.Set();
			logger.Info("Stopped");
		}

		private static void onStop(object sender, EventArgs e) {
			Console.WriteLine("Stop requested");
			_shutdownToken.Cancel();
			_shutdownCompletedEvent.Wait();
		}
	}
}
