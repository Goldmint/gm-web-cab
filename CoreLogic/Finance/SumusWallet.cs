using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance {

	public static class SumusWallet {

		public static async Task<bool> Charge(IServiceProvider services, long userId, decimal amount, SumusToken token) {
			if (amount <= 0) return false;

			var logger = services.GetLoggerFor(typeof(SumusWallet));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var field = "";
			switch (token) {
				case SumusToken.Gold: field = "balance_gold"; break;
				case SumusToken.Mnt: field = "balance_mnt"; break;
				default: return false;
			}

			using (var tx = await dbContext.Database.BeginTransactionAsync()) {
				try {
					decimal bal = 0;
					var conn = dbContext.Database.GetDbConnection();
					using (var cmd = conn.CreateCommand()) {
						cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
						cmd.CommandText = $"SELECT `{field}` FROM `gm_user_sumus_wallet` WHERE `user_id`={userId} FOR UPDATE";
						var b = await cmd.ExecuteScalarAsync() as decimal?;
						if (b != null) bal = b.Value;
						else return false;
					}
					if (bal < amount) {
						return false;
					}
					bal -= amount;
					using (var cmd = conn.CreateCommand()) {
						cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
						cmd.CommandText = $"UPDATE `gm_user_sumus_wallet` SET `{field}`={bal.ToString(System.Globalization.CultureInfo.InvariantCulture)} WHERE `user_id`={userId}";
						await cmd.ExecuteNonQueryAsync();
					}
					tx.Commit();
					return true;
				} catch (Exception e) {
					logger.Error(e, $"Failed to charge user #{userId}");
				}
			}
			return false;
		}

		public static async Task<bool> Refill(IServiceProvider services, long userId, decimal amount, SumusToken token) {
			if (amount <= 0) return false;

			var logger = services.GetLoggerFor(typeof(SumusWallet));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var field = "";
			switch (token) {
				case SumusToken.Gold: field = "balance_gold"; break;
				case SumusToken.Mnt: field = "balance_mnt"; break;
				default: return false;
			}

			using (var tx = await dbContext.Database.BeginTransactionAsync()) {
				try {
					decimal bal = 0;
					var conn = dbContext.Database.GetDbConnection();
					using (var cmd = conn.CreateCommand()) {
						cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
						cmd.CommandText = $"SELECT `{field}` FROM `gm_user_sumus_wallet` WHERE `user_id`={userId} FOR UPDATE";
						var b = await cmd.ExecuteScalarAsync() as decimal?;
						if (b != null) bal = b.Value;
						else return false;
					}
					bal += amount;
					using (var cmd = conn.CreateCommand()) {
						cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
						cmd.CommandText = $"UPDATE `gm_user_sumus_wallet` SET `{field}`={bal.ToString(System.Globalization.CultureInfo.InvariantCulture)} WHERE `user_id`={userId}";
						await cmd.ExecuteNonQueryAsync();
					}
					tx.Commit();
					return true;
				} catch (Exception e) {
					logger.Error(e, $"Failed to refill user #{userId}");
				}
			}
			return false;
		}
	}
}
