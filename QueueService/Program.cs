using Goldmint.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog.Web;

namespace Goldmint.QueueService {

	public partial class Program {

		[Flags]
		public enum WorkingMode : int {
			Worker = 1,
			Core = 2,
		}

		private const string IpcPipeName = "gm.queueservice.ipc";

		public static WorkingMode Mode { get; private set; }

		private static IConfiguration _configuration;
		private static AppConfig _appConfig;
		private static LogFactory _loggerFactory;
		private static IHostingEnvironment _environment;

		private static CancellationTokenSource _shutdownToken;
		private static ManualResetEventSlim _shutdownCompletedEvent;
		private static NamedPipeServerStream _ipcServer;
		private static object _ipcServerMonitor;

		// ---

		/// <summary>
		/// Entry point
		/// </summary>
		/// <param name="args">
		/// `ipc-stop` - command to stop launched instance;
		/// </param>
		public static void Main(string[] args) {

			if (SetupIpc(args)) {
				return;
			}

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
					.AddJsonFile($"appsettings.{_environment.EnvironmentName}.Private.json", optional: true)
					.Build()
				;

				_appConfig = new AppConfig();
				_configuration.Bind(_appConfig);
			}
			catch (Exception e) {
				throw new Exception("Failed to get app settings", e);
			}

			// nlog
			var nlogConfig = new NLog.Config.XmlLoggingConfiguration($"nlog.{_environment.EnvironmentName}.config");
			_loggerFactory = new LogFactory(nlogConfig);
			LogManager.Configuration = nlogConfig;

			var logger = _loggerFactory.GetLogger(typeof(Program).FullName);
			logger.Info("Launched");

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

			// setup ms logger
			services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().AddNLog();

			// setup workers and wait
			Task.WaitAll(SetupWorkers(services).ToArray());

			OnStopped();

			_shutdownCompletedEvent.Set();
			logger.Info("Stopped");
		}

		private static void OnStopped() {
			StopCommonServices();
		}

		// ---

		/// <summary>
		/// IPC server/client 
		/// </summary>
		private static bool SetupIpc(string[] args) {

			bool isClient = args.Contains("ipc-stop");

			// process that listens for commands
			if (!isClient) {

				_ipcServerMonitor = new object();
				_ipcServer = new NamedPipeServerStream(IpcPipeName);

				_ipcServer.BeginWaitForConnection(
					(result) => {
						_ipcServer.EndWaitForConnection(result);

						lock (_ipcServerMonitor) {
							_shutdownToken.Cancel();
							_shutdownCompletedEvent.Wait();
							_ipcServer.Close();
						}
					},
					_ipcServer
				);

				return false;
			}

			// process that sends command
			try {
				using (var pipe = new NamedPipeClientStream(IpcPipeName)) {
					pipe.Connect(60000);
					pipe.ReadByte();
				}
			}
			catch { }

			return true;
		}
		
	}
}
