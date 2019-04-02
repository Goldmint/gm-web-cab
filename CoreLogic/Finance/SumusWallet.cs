using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Finance {

	public static class SumusWallet {

		public static async Task<bool> ChangeGoldBalance(IServiceProvider services, long userId, decimal delta) {

			var logger = services.GetLoggerFor(typeof(SumusWallet));
			var mutexHolder = services.GetRequiredService<IMutexHolder>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var mutexBuilder =
				new MutexBuilder(mutexHolder)
				.Mutex(MutexEntity.SumusWalletBalance, userId)
			;

			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var wallet = await (
						from w in dbContext.UserSumusWallet
						where w.UserId == userId
						select w
					)
						.AsNoTracking()
						.FirstOrDefaultAsync()
					;
					if (wallet != null && delta != 0) {
						if (delta < 0 && wallet.BalanceGold < -delta) {
							return false;
						}
						wallet.BalanceGold += delta;
						dbContext.Update(wallet);
						await dbContext.SaveChangesAsync();
						return true;
					}
				}
				return false;
			});
		}
	}
}
