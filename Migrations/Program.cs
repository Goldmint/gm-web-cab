using System;
using System.IO;
using System.Text;
using Goldmint.Common;
using Goldmint.Common.Sumus;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Goldmint.Migrations {

	public static class Program {
		public static void Main(string[] args) {
			if (args[0] == "user-sumus-wallet-gen") {
				var ctx = new ApplicationDbContextFactory().CreateDbContext(new string[]{ });
				var userz = ctx.Users.ToListAsync().Result;
				foreach (var u in userz) {
					var s = new Signer();
					var w = new UserSumusWallet() {
						UserId = u.Id,
						PublicKey = s.PublicKey,
						PrivateKey = s.PrivateKey,
						TimeCreated = DateTime.UtcNow,
						TimeChecked = DateTime.UtcNow,
					};
					ctx.UserSumusWallet.Add(w);
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
