using Goldmint.Common;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic {

	public static class UserAccount {

		/// <summary>
		/// User ID to long value: "u0001" => 1L, "2" => 2L
		/// </summary>
		public static long ExtractId(string data) {
			var ret = 0L;
			if (!string.IsNullOrWhiteSpace(data) && (data[0] == 'u' || char.IsDigit(data[0]))) {
				var digits = string.Join("", data.Where(c => char.IsDigit(c)).Select(c => c.ToString()).ToArray()).TrimStart('0');
				long.TryParse(digits, out ret);
			}
			return ret;
		}

		public static bool IsUserVerifiedL0(User user) {
			return (user?.UserVerification?.FirstName?.Length ?? 0) > 0 && (user?.UserVerification?.LastName?.Length ?? 0) > 0;
		}

		public static bool IsUserVerifiedL1(User user) {
			return user?.UserVerification?.KycShuftiProTicketId != null;
		}

		public sealed class FiatLimits {

			public Limits CurrentUser { get; set; }
			public Limits Level0 { get; set; }
			public Limits Level1 { get; set; }

			public class Limits {

				public long DayDeposit { get; internal set; }
				public long MonthDeposit { get; internal set; }
				public long DayWithdraw { get; internal set; }
				public long MonthWithdraw { get; internal set; }
			}
		}

		public static Task<FiatLimits> GetFiatLimits(IServiceProvider services, FiatCurrency currency, User user) {

			var appConfig = services.GetRequiredService<AppConfig>();

			if (currency != FiatCurrency.USD) {
				throw new NotImplementedException("Non-USD currency is not implemented");
			}

			var current = new FiatLimits.Limits();
			var level0 = new FiatLimits.Limits();
			var level1 = new FiatLimits.Limits();

			// usd
			if (currency == FiatCurrency.USD) {

				level0 = new FiatLimits.Limits() {
					DayDeposit = appConfig.Constants.FiatAccountLimitsUSD.L0.DayDeposit,
					MonthDeposit = appConfig.Constants.FiatAccountLimitsUSD.L0.MonthDeposit,
					DayWithdraw = appConfig.Constants.FiatAccountLimitsUSD.L0.DayWithdraw,
					MonthWithdraw = appConfig.Constants.FiatAccountLimitsUSD.L0.MonthWithdraw,
				};

				level1 = new FiatLimits.Limits() {
					DayDeposit = appConfig.Constants.FiatAccountLimitsUSD.L1.DayDeposit,
					MonthDeposit = appConfig.Constants.FiatAccountLimitsUSD.L1.MonthDeposit,
					DayWithdraw = appConfig.Constants.FiatAccountLimitsUSD.L1.DayWithdraw,
					MonthWithdraw = appConfig.Constants.FiatAccountLimitsUSD.L1.MonthWithdraw,
				};
			}

			// level 0
			if (IsUserVerifiedL0(user)) current = level0;
			
			// level 1
			if (IsUserVerifiedL1(user)) current = level1;

			return Task.FromResult(
				new FiatLimits() {
					CurrentUser = current,
					Level0 = level0,
					Level1 = level1,
				}
			);
		}

		/// <summary>
		/// Current deposit limit for specified user
		/// </summary>
		public static async Task<long> GetCurrentFiatDepositLimit(IServiceProvider services, FiatCurrency currency, User user) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			// load user verification
			if (user.UserVerification == null) {
				await dbContext.Entry(user).Reference(_ => _.UserVerification).LoadAsync();
			}

			var startDateMonth = DateTime.UtcNow.AddDays(-30);
			var startDateDay = DateTime.UtcNow.AddHours(-24);
			var accountLimits = await GetFiatLimits(services, currency, user);

			// get all user deposits for last 30 days
			var deposits = await (
				from d in dbContext.Deposit
				where
				d.UserId == user.Id &&
				d.Currency == currency &&
				d.TimeCreated >= startDateMonth &&
				d.Status != DepositStatus.Failed // get all pending or successful deposits
				select d
			).AsNoTracking().ToListAsync();

			var amountMonth = 0L;
			var amountDay = 0L;
			foreach (var d in deposits) {
				if (d.TimeCreated >= startDateDay) amountDay += d.AmountCents;
				amountMonth += d.AmountCents;
			}

			return Math.Max(0, Math.Min(accountLimits.CurrentUser.DayDeposit - amountDay, accountLimits.CurrentUser.MonthDeposit - amountMonth));
		}

		/// <summary>
		/// Current withdraw limit for specified user
		/// </summary>
		public static async Task<long> GetCurrentFiatWithdrawLimit(IServiceProvider services, FiatCurrency currency, User user) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			// load user verification
			if (user.UserVerification == null) {
				await dbContext.Entry(user).Reference(_ => _.UserVerification).LoadAsync();
			}

			var startDateMonth = DateTime.UtcNow.AddDays(-30);
			var startDateDay = DateTime.UtcNow.AddHours(-24);
			var accountLimits = await GetFiatLimits(services, currency, user);

			// get all user withdraws for last 30 days
			var withdraws = await (
				from d in dbContext.Withdraw
				where
				d.UserId == user.Id &&
				d.Currency == currency &&
				d.TimeCreated >= startDateMonth &&
				d.Status != WithdrawStatus.Failed // get all pending or successful
				select d
			)
				.AsNoTracking()
				.ToListAsync()
			;

			var amountMonth = 0L;
			var amountDay = 0L;
			foreach (var d in withdraws) {
				if (d.TimeCreated >= startDateDay) amountDay += d.AmountCents;
				amountMonth += d.AmountCents;
			}

			return Math.Max(0, Math.Min(accountLimits.CurrentUser.DayWithdraw - amountDay, accountLimits.CurrentUser.MonthWithdraw - amountMonth));
		}

		/// <summary>
		/// Persist user activity record
		/// </summary>
		public static async Task SaveActivity(IServiceProvider services, User user, UserActivityType type, string comment, string ip, string agent) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var activity = new DAL.Models.UserActivity() {
				User = user,
				Ip = ip,
				Agent = agent,
				Type = type.ToString().ToLowerInvariant(),
				Comment = comment,
				TimeCreated = DateTime.UtcNow,
			};

			dbContext.Add(activity);
			await dbContext.SaveChangesAsync();
		}
	}
}
