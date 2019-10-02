﻿using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.DAL {

	public class ApplicationDbContext : IdentityDbContext<User, Role, long, UserClaim, UserRole, UserLogin, RoleClaim, UserToken> {

		public DbSet<BannedCountry> BannedCountry { get; set; }
		public DbSet<KycTicket> KycShuftiProTicket { get; set; }
		public DbSet<Notification> Notification { get; set; }
		public DbSet<Settings> Settings { get; set; }
		public DbSet<EthSending> EthSending { get; set; }
		public DbSet<Transparency> Transparency { get; set; }
		public DbSet<TransparencyStat> TransparencyStat { get; set; }
		public DbSet<UserActivity> UserActivity { get; set; }
		public DbSet<UserFinHistory> UserFinHistory { get; set; }
		public DbSet<UserOptions> UserOptions { get; set; }
		public DbSet<UserVerification> UserVerification { get; set; }
		public DbSet<UserLimits> UserLimits { get; set; }
		public DbSet<PoolFreezeRequest> PoolFreezeRequest { get; set; }
		public DbSet<UserSumusWallet> UserSumusWallet { get; set; }
		public DbSet<SellGoldEth> SellGoldEth { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder builder) {
			base.OnModelCreating(builder);

			builder.Entity<User>(entity => { entity.ToTable(name: "gm_user"); });
			builder.Entity<Role>(entity => { entity.ToTable(name: "gm_role"); });
			builder.Entity<UserRole>(entity => { entity.ToTable("gm_user_role"); });
			builder.Entity<UserClaim>(entity => { entity.ToTable("gm_user_claim"); });
			builder.Entity<UserLogin>(entity => { entity.ToTable("gm_user_login"); });
			builder.Entity<UserToken>(entity => { entity.ToTable("gm_user_token"); });
			builder.Entity<RoleClaim>(entity => { entity.ToTable("gm_role_claim"); });

			// for currency amount
			foreach (var property in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))) {
				property.Relational().ColumnType = "decimal(38, 18)";
			}
		}

		// ---

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

		public async Task<string> GetDbSetting(Common.DbSetting key, string def) {

			string keyStr = key.ToString();
			if (keyStr.Length > DAL.Models.Settings.MaxKeyFieldLength) {
				throw new Exception("DB settings key is too long");
			}

			try {
				var sett = await (
						from s in this.Settings
						where s.Key == keyStr
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

			string keyStr = key.ToString();
			if (keyStr.Length > DAL.Models.Settings.MaxKeyFieldLength) {
				throw new Exception("DB settings key is too long");
			}
			if (value != null && value.Length > DAL.Models.Settings.MaxValueFieldLength) {
				throw new Exception("DB settings value is too long");
			}

			try {
				sett = await (
						from s in this.Settings
						where s.Key == keyStr
						select s
					)
					.FirstOrDefaultAsync();

				if (sett != null) {
					sett.Value = value;
				}
				else {
					sett = new Settings() {
						Key = keyStr,
						Value = value,
					};

					this.Settings.Add(sett);
				}

				await SaveChangesAsync();

				return true;
			}
			catch {
			}
			finally {
				if (sett != null) this.Entry(sett).State = EntityState.Detached;
			}

			return false;
		}
	}
}
