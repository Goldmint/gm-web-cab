using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.IO.Pipes;
using System.Linq;

namespace Goldmint.WebApplication {

	public class Program {

		private const string IPCPipeName = "gm.webapplication.ipc";

		private static IWebHost _webHost;
		private static NamedPipeServerStream _ipcServer;
		private static object _ipcServerMonitor;

		public static void Main(string[] args) {

			if (SetupIPC(args)) {
				return;
			}

			_webHost = BuildWebHost(args);
			_webHost.Run();
		}

		public static IWebHost BuildWebHost(string[] args) {
			return WebHost.CreateDefaultBuilder(args)
				.UseKestrel(opts => {
					opts.AddServerHeader = false;
					opts.UseSystemd();
					opts.AllowSynchronousIO = true;
					opts.ApplicationSchedulingMode = Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.SchedulingMode.ThreadPool;
				})
				.UseLibuv()
				.UseStartup<Startup>()
				.Build()
			;
		}

		private static bool SetupIPC(string[] args) {

			bool isClient = args.Contains("ipc-stop");

			if (!isClient) {

				_ipcServerMonitor = new object();
				_ipcServer = new NamedPipeServerStream(IPCPipeName);

				_ipcServer.BeginWaitForConnection(
					(result) => {
						_ipcServer.EndWaitForConnection(result);

						lock (_ipcServerMonitor) {
							_webHost.StopAsync();
							_webHost.WaitForShutdown();
							_ipcServer.Close();
						}
					},
					_ipcServer
				);

				return false;
			}

			if (isClient) {

				try {
					using (var pipe = new NamedPipeClientStream(IPCPipeName)) {
						pipe.Connect(60000);
						pipe.ReadByte();
					}
				}
				catch { }

				return true;
			}

			return false;
		}
	}
}
