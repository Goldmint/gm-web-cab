using System;
using System.IO;
using System.Text;
using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Goldmint.Migrations {

	public class Program {
		public static void Main(string[] args) {
		}
	}

	public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {

		public ApplicationDbContext CreateDbContext(string[] args) {
			Setup();

			var opts = new DbContextOptionsBuilder<ApplicationDbContext>();

			Console.WriteLine($"Using connection: {_appConfig.ConnectionStrings.Default}");

			opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
				myopts.UseRelationalNulls(true);
			});

			return new ApplicationDbContext(opts.Options);
		}

		private IConfiguration _configuration;
		private AppConfig _appConfig;
		private string _environment;

		private void Setup() {

			_environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			var curDir = Directory.GetCurrentDirectory();
			Console.OutputEncoding = Encoding.UTF8;

			// config
			try {
				var cfgDir = Environment.GetEnvironmentVariable("ASPNETCORE_CONFIGPATH");

				_configuration = new ConfigurationBuilder()
						.SetBasePath(cfgDir)
						.AddJsonFile("appsettings.json", optional: false)
						.AddJsonFile($"appsettings.{_environment}.json", optional: false)
						.Build()
					;

				_appConfig = new AppConfig();
				_configuration.Bind(_appConfig);
			}
			catch (Exception e) {
				throw new Exception("Failed to get app settings", e);
			}

			var dbCustomConnection = Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION");
			if (!string.IsNullOrWhiteSpace(dbCustomConnection)) {
				_appConfig.ConnectionStrings.Default = dbCustomConnection;
			}
		}
	}
}
