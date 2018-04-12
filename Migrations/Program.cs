using System;
using System.IO;
using System.Text;
using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Goldmint.Migrations {

	public static class Program {
		public static void Main(string[] args) {
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
