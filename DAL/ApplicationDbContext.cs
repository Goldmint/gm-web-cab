using System.Collections.Generic;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Goldmint.DAL {

	public class ApplicationDbContext : IdentityDbContext<User, Role, long, UserClaim, UserRole, UserLogin, RoleClaim, UserToken> {

		public DbSet<Settings> Settings { get; set; }
		public DbSet<UserOptions> UserOptions { get; set; }
		public DbSet<KycTicket> KycShuftiProTicket { get; set; }
		public DbSet<UserVerification> UserVerification { get; set; }
		public DbSet<Models.Mutex> Mutex { get; set; }
		public DbSet<Card> Card { get; set; }
		public DbSet<CardPayment> CardPayment { get; set; }
		public DbSet<SwiftRequest> SwiftRequest { get; set; }
		public DbSet<Deposit> Deposit { get; set; }
		public DbSet<Withdraw> Withdraw { get; set; }
		public DbSet<Notification> Notification { get; set; }
		public DbSet<UserActivity> UserActivity { get; set; }
		public DbSet<BuyRequest> BuyRequest { get; set; }
		public DbSet<SellRequest> SellRequest { get; set; }
		public DbSet<TransferRequest> TransferRequest { get; set; }
		public DbSet<FinancialHistory> FinancialHistory { get; set; }
		public DbSet<UserOpLog> UserOpLog { get; set; }
		public DbSet<Transparency> Transparency { get; set; }
		public DbSet<BannedCountry> BannedCountry { get; set; }
		public DbSet<SignedDocument> SignedDocument { get; set; }
		public DbSet<CryptoDeposit> CryptoDeposit { get; set; }
		public DbSet<TransparencyStat> TransparencyStat { get; set; }
		public DbSet<SwiftTemplate> SwiftTemplate { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
		}

		protected override void OnModelCreating(ModelBuilder builder) {
			base.OnModelCreating(builder);

			builder.Entity<Role>().ToTable("gm_role");
			builder.Entity<RoleClaim>().ToTable("gm_role_claim");
			builder.Entity<User>().ToTable("gm_user");
			builder.Entity<UserClaim>().ToTable("gm_user_claim");
			builder.Entity<UserLogin>().ToTable("gm_user_login");
			builder.Entity<UserRole>().ToTable("gm_user_role");
			builder.Entity<UserToken>().ToTable("gm_user_token");

			// for currency amount
			foreach (var property in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(decimal))) {
				property.Relational().ColumnType = "decimal(26, 2)";
			}
		}

		// ---

		/*public void Detach(object entity) {
			this.Entry(entity).State = EntityState.Detached;
		}*/

		public void DetachEverything() {
			var entries = this.ChangeTracker
				.Entries()
				.Where(e => e.State != EntityState.Detached)
				.ToList()
			;
			foreach (var v in entries) {
				this.Entry(v.Entity).State = EntityState.Detached;
			}
		}
		
		public override int SaveChanges() {
			UpdateConcurrencyStamps();
			return base.SaveChanges();
		}

		public override int SaveChanges(bool acceptAllChangesOnSuccess) {
			UpdateConcurrencyStamps();
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}

		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) {
			UpdateConcurrencyStamps();
			return base.SaveChangesAsync(cancellationToken);
		}

		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) {
			UpdateConcurrencyStamps();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		private void UpdateConcurrencyStamps() {

			var entries = ChangeTracker.Entries();
			foreach (var e in entries) {
				if ((e.State == EntityState.Added || e.State == EntityState.Modified)) {
					(e.Entity as IConcurrentUpdate)?.OnConcurrencyStampRegen();
				}
			}
		}


		// ---

		public async Task<string> GetDBSetting(Common.DbSetting key, string def) {
			Settings sett = null;

			try {
				sett = await (
					from s in this.Settings
					where s.Key == key.ToString()
					select s
				)
				.AsNoTracking()
				.FirstOrDefaultAsync();

				if (sett != null) {
					return sett.Value;
				}
			}
			catch { }

			return def;
		}

		public async Task<bool> SaveDbSetting(Common.DbSetting key, string value) {
			Settings sett = null;

			try {
				sett = await (
					from s in this.Settings
					where s.Key == key.ToString()
					select s
				)
				.FirstOrDefaultAsync();

				if (sett != null) {
					sett.Value = value;
				}
				else {
					sett = new Settings() {
						Key = key.ToString(),
						Value = value,
					};

					this.Settings.Add(sett);
				}

				await SaveChangesAsync();

				return true;
			}
			catch { }
			finally {
				if (sett != null) this.Entry(sett).State = EntityState.Detached;
			}
			return false;
		}
	}
}
