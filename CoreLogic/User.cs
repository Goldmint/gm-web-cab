using Goldmint.Common;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.CoreLogic {

	public static class User {

		public static readonly TimeSpan FinancialHistoryOvarlappingOperationsAllowedWithin = TimeSpan.FromMinutes(60);

		public static bool HasSignedDpa(UserOptions data) {
			return
				data?.DPADocument != null &&
				data.DPADocument.Type == SignedDocumentType.Dpa &&
				data.DPADocument.TimeCompleted != null &&
				data.DPADocument.IsSigned
			;
		}

		public static bool HasFilledPersonalData(UserVerification data) {
			return
				(data?.FirstName?.Length ?? 0) > 0 &&
				(data?.LastName?.Length ?? 0) > 0
			;
		}

		public static bool HasKYCVerification(UserVerification data) {
			return
				data?.LastKycTicket != null &&
				data.LastKycTicket.TimeResponded != null &&
				data.LastKycTicket.IsVerified
			;
		}

		public static bool HasSignedAgreement(UserVerification data) {
			return
				data?.LastAgreement != null &&
				data.LastAgreement.Type == SignedDocumentType.Tos &&
				data.LastAgreement.TimeCompleted != null &&
				data.LastAgreement.IsSigned
			;
		}

		// ---

		/// <summary>
		/// User's tier
		/// </summary>
		public static UserTier GetTier(DAL.Models.Identity.User user) {
			var tier = UserTier.Tier0;

			var hasDpa = HasSignedDpa(user?.UserOptions);
			var hasPersData = HasFilledPersonalData(user?.UserVerification);
			var hasKyc = HasKYCVerification(user?.UserVerification);
			var hasAgreement = HasSignedAgreement(user?.UserVerification);

			if (hasDpa && hasPersData) tier = UserTier.Tier1;

			if (hasKyc && hasAgreement) tier = UserTier.Tier2;

			return tier;
		}

		// ---

		/// <summary>
		/// Fiat limits by level plus specified user's level
		/// </summary>
		public static Task<FiatLimitsLevels> GetFiatLimits(IServiceProvider services, FiatCurrency currency, UserTier userTier) {

			var appConfig = services.GetRequiredService<AppConfig>();

			if (currency != FiatCurrency.USD) {
				throw new NotImplementedException("Non-USD currency is not implemented");
			}

			var current = new User.FiatOperationsLimits();
			var tier0Limits = new User.FiatOperationsLimits();
			var tier1Limits = new User.FiatOperationsLimits();
			var tier2Limits = new User.FiatOperationsLimits();

			// usd
			if (currency == FiatCurrency.USD) {

				tier1Limits = new User.FiatOperationsLimits() {
					Deposit = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUsd.Tier1.DayDeposit,
						Month = appConfig.Constants.FiatAccountLimitsUsd.Tier1.MonthDeposit,
					},
					Withdraw = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUsd.Tier1.DayWithdraw,
						Month = appConfig.Constants.FiatAccountLimitsUsd.Tier1.MonthWithdraw,
					}
				};

				tier2Limits = new User.FiatOperationsLimits() {
					Deposit = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUsd.Tier2.DayDeposit,
						Month = appConfig.Constants.FiatAccountLimitsUsd.Tier2.MonthDeposit,
					},
					Withdraw = new FiatLimits() {
						Day = appConfig.Constants.FiatAccountLimitsUsd.Tier2.DayWithdraw,
						Month = appConfig.Constants.FiatAccountLimitsUsd.Tier2.MonthWithdraw,
					}
				};
			}

			if (userTier == UserTier.Tier1) current = tier1Limits;
			if (userTier == UserTier.Tier2) current = tier2Limits;

			return Task.FromResult(
				new FiatLimitsLevels() {
					Current = current,
					Tier0 = tier0Limits,
					Tier1 = tier1Limits,
					Tier2 = tier2Limits,
				}
			);
		}

		/// <summary>
		/// Current deposit limit for specified user
		/// </summary>
		public static async Task<FiatLimits> GetCurrentFiatDepositLimit(IServiceProvider services, FiatCurrency currency, long userId, UserTier userTier) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var startDateMonth = DateTime.UtcNow.AddDays(-30);
			var startDateDay = DateTime.UtcNow.AddHours(-24);
			var accountLimits = await GetFiatLimits(services, currency, userTier);

			// get all user deposits for last 30 days
			var deposits = await (
				from d in dbContext.Deposit
				where
				d.UserId == userId &&
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
		public static async Task<FiatLimits> GetCurrentFiatWithdrawLimit(IServiceProvider services, FiatCurrency currency, long userId, UserTier userTier) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var startDateMonth = DateTime.UtcNow.AddDays(-30);
			var startDateDay = DateTime.UtcNow.AddHours(-24);
			var accountLimits = await GetFiatLimits(services, currency, userTier);

			// get all user withdraws for last 30 days
			var withdraws = await (
				from d in dbContext.Withdraw
				where
				d.UserId == userId &&
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
		public static async Task SaveActivity(IServiceProvider services, DAL.Models.Identity.User user, UserActivityType type, string comment, string ip, string agent) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var activity = new DAL.Models.UserActivity() {
				UserId = user.Id,
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
		public static async Task<bool> HasPendingBlockchainOps(IServiceProvider services, long userId) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var notTime = DateTime.UtcNow;

			return await dbContext.FinancialHistory.Where(_ =>
				_.UserId == userId &&
				_.Status == FinancialHistoryStatus.Processing &&
				(notTime - _.TimeCreated) < FinancialHistoryOvarlappingOperationsAllowedWithin
			).CountAsync() > 0;
		}

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

		// ---

		/// <summary>
		/// Levels
		/// </summary>
		public sealed class FiatLimitsLevels {

			public FiatOperationsLimits Current { get; internal set; }
			public FiatOperationsLimits Tier0 { get; internal set; }
			public FiatOperationsLimits Tier1 { get; internal set; }
			public FiatOperationsLimits Tier2 { get; internal set; }

			internal FiatLimitsLevels() {
				Current = new FiatOperationsLimits();
				Tier0 = new FiatOperationsLimits();
				Tier1 = new FiatOperationsLimits();
				Tier2 = new FiatOperationsLimits();
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
