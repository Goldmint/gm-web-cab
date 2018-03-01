using Goldmint.Common;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.CoreLogic {

	public static class UserAccount {

		/// <summary>
		/// User ID to long value: "u0001" => 1L, "2" => 2L
		/// </summary>
		public static long ExtractId(string data) {
			var ret = 0L;
			if (!string.IsNullOrWhiteSpace(data) && (data[0] == 'u' || char.IsDigit(data[0]))) {
				var digits = string.Join("", data.Where(char.IsDigit).Select(c => c.ToString()).ToArray()).TrimStart('0');
				long.TryParse(digits, out ret);
			}
			return ret;
		}

		public static bool IsUserVerifiedL0(User user) {
			return 
				(user?.UserVerification?.FirstName?.Length ?? 0) > 0 && 
				(user?.UserVerification?.LastName?.Length ?? 0) > 0 &&
				(user?.UserVerification?.SignedAgreementId != null)
			;
		}

		public static bool IsUserVerifiedL1(User user) {
			return 
				IsUserVerifiedL0(user) &&
				user?.UserVerification?.KycShuftiProTicketId != null
			;
		}

		/// <summary>
		/// Fiat limits by level plus specified user's level
		/// </summary>
		public static Task<FiatLimitsLevels> GetFiatLimits(IServiceProvider services, FiatCurrency currency, User user) {

			var appConfig = services.GetRequiredService<AppConfig>();

			if (currency != FiatCurrency.USD) {
				throw new NotImplementedException("Non-USD currency is not implemented");
			}

			var current = new UserAccount.FiatOperationsLimits();
			var level0 = new UserAccount.FiatOperationsLimits();
			var level1 = new UserAccount.FiatOperationsLimits();

			// usd
			if (currency == FiatCurrency.USD) {

				level0 = new UserAccount.FiatOperationsLimits() {
					Deposit = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUSD.L0.DayDeposit,
						Month = appConfig.Constants.FiatAccountLimitsUSD.L0.MonthDeposit,
					},
					Withdraw = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUSD.L0.DayWithdraw,
						Month = appConfig.Constants.FiatAccountLimitsUSD.L0.MonthWithdraw,
					}
				};

				level1 = new UserAccount.FiatOperationsLimits() {
					Deposit = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUSD.L1.DayDeposit,
						Month = appConfig.Constants.FiatAccountLimitsUSD.L1.MonthDeposit,
					},
					Withdraw = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUSD.L1.DayWithdraw,
						Month = appConfig.Constants.FiatAccountLimitsUSD.L1.MonthWithdraw,
					}
				};
			}

			// level 0
			if (IsUserVerifiedL0(user)) current = level0;

			// level 1
			if (IsUserVerifiedL1(user)) current = level1;

			return Task.FromResult(
				new FiatLimitsLevels() {
					Current = current,
					Level0 = level0,
					Level1 = level1,
				}
			);
		}

		/// <summary>
		/// Current deposit limit for specified user
		/// </summary>
		public static async Task<FiatLimits> GetCurrentFiatDepositLimit(IServiceProvider services, FiatCurrency currency, User user) {

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
				select new { AmountCents = d.AmountCents, TimeCreated = d.TimeCreated }
			).AsNoTracking().ToListAsync();

			var amountMonth = 0L;
			var amountDay = 0L;
			foreach (var d in deposits) {
				if (d.TimeCreated >= startDateDay) amountDay += d.AmountCents;
				amountMonth += d.AmountCents;
			}

			return new FiatLimits() {
				Day = Math.Max(0, accountLimits.Current.Deposit.Day - amountDay),
				Month = Math.Max(0, accountLimits.Current.Deposit.Month - amountMonth),
			};
		}

		/// <summary>
		/// Current withdraw limit for specified user
		/// </summary>
		public static async Task<FiatLimits> GetCurrentFiatWithdrawLimit(IServiceProvider services, FiatCurrency currency, User user) {

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
				select new { AmountCents = d.AmountCents, TimeCreated = d.TimeCreated }
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

			return new FiatLimits() {
				Day = Math.Max(0, accountLimits.Current.Withdraw.Day - amountDay),
				Month = Math.Max(0, accountLimits.Current.Withdraw.Month - amountMonth),
			};
		}

		/// <summary>
		/// Persist user activity record
		/// </summary>
		public static async Task SaveActivity(IServiceProvider services, User user, UserActivityType type, string comment, string ip, string agent) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var activity = new DAL.Models.UserActivity() {
				User = user,
				Ip = ip,
				Agent = agent.LimitLength(128),
				Type = type.ToString().ToLowerInvariant(),
				Comment = comment.LimitLength(512),
				TimeCreated = DateTime.UtcNow,
			};

			dbContext.Add(activity);
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Check for pending operations
		/// </summary>
		public static async Task<bool> HasPendingBlockchainOps(IServiceProvider services, User user) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			return await dbContext.FinancialHistory.Where(_ =>
				_.UserId == user.Id &&
				_.Status == FinancialHistoryStatus.Pending
			).CountAsync() > 0;
		}

		// ---

		/// <summary>
		/// Levels
		/// </summary>
		public sealed class FiatLimitsLevels {

			public FiatOperationsLimits Current { get; internal set; }
			public FiatOperationsLimits Level0 { get; internal set; }
			public FiatOperationsLimits Level1 { get; internal set; }

			internal FiatLimitsLevels() {
				Current = new FiatOperationsLimits();
				Level0 = new FiatOperationsLimits();
				Level1 = new FiatOperationsLimits();
			}
		}

		/// <summary>
		/// Limited operations
		/// </summary>
		public sealed class FiatOperationsLimits {

			public FiatLimits Deposit { get; internal set; }
			public FiatLimits Withdraw { get; internal set; }

			internal FiatOperationsLimits() {
				Deposit = new FiatLimits();
				Withdraw = new FiatLimits();
			}
		}

		/// <summary>
		/// Limits types
		/// </summary>
		public sealed class FiatLimits {

			public long Day { get; internal set; }
			public long Month { get; internal set; }

			public long Minimal => Math.Max(0, Math.Min(Day, Month));
			public long Maximal => Math.Max(0, Math.Max(Day, Month));

			internal FiatLimits() { }
		}
	}
}
