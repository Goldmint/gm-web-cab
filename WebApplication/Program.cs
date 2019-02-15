using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Goldmint.WebApplication {

	public class Program {

		private static IWebHost _webHost;

		public static void Main(string[] args) {
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
	}
}
