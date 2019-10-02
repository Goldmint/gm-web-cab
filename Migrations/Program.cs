using System;
using System.IO;
using System.Text;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Goldmint.Migrations {

	public static class Program {
		public static void Main(string[] args) {
			if (args[0] == "retrack-mint-deposit-wallets") {
				var ctx = new ApplicationDbContextFactory().CreateDbContext(new string[]{ });
				var wallets = ctx.UserSumusWallet.AsTracking().ToListAsync().Result;
				foreach (var w in wallets) {
					w.Tracking = false;
				}
				ctx.SaveChanges(true);
			}
		}
	}

	public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {

		public ApplicationDbContext CreateDbContext(string[] args) {
			Setup();

			var opts = new DbContextOptionsBuilder<ApplicationDbContext>();

			Console.WriteLine($"Using connection: { Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION") }");

			opts.UseMySql(Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION") ?? "", myopts => {
				myopts.UseRelationalNulls(true);
			});

			return new ApplicationDbContext(opts.Options);
		}

		private void Setup() {

			var curDir = Directory.GetCurrentDirectory();
			Console.OutputEncoding = Encoding.UTF8;

			if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION"))) {
				throw new Exception("ASPNETCORE_DBCONNECTION argument is not specified");
			}
		}
	}
}
