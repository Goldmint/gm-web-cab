using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL.Models;

namespace Goldmint.CoreLogic {

	public static class User {

		public static readonly TimeSpan FinancialHistoryOvarlappingOperationsAllowedWithin = TimeSpan.FromMinutes(60);

		public static bool HasSignedDpa(DAL.Models.UserOptions data) {
			return
				data?.DpaDocument != null &&
				data.DpaDocument.Type == SignedDocumentType.Dpa &&
				data.DpaDocument.TimeCompleted != null &&
				data.DpaDocument.IsSigned
			;
		}

		public static bool HasFilledPersonalData(DAL.Models.UserVerification data) {
			return
				(data?.FirstName?.Length ?? 0) > 0 &&
				(data?.LastName?.Length ?? 0) > 0
			;
		}

		public static bool HasKycVerification(DAL.Models.UserVerification data) {
			return
				data?.LastKycTicket != null &&
				data.LastKycTicket.TimeResponded != null &&
				data.LastKycTicket.IsVerified
			;
		}

		public static bool HasTosSigned(DAL.Models.UserVerification data) {
			return
				data?.AgreedWithTos != null &&
				data.AgreedWithTos.Value
			;
		}

		public static bool HasProvedResidence(DAL.Models.UserVerification data, bool residenceRequried) {
			return
			    residenceRequried
                //data?.ProvedResidence ?? false
                ;
		}

		// ---

		/// <summary>
		/// User's tier
		/// </summary>
		public static UserTier GetTier(DAL.Models.Identity.User user, RuntimeConfig rc) {
			var tier = UserTier.Tier0;

			var hasDpa = HasSignedDpa(user?.UserOptions);
			var hasPersData = HasFilledPersonalData(user?.UserVerification);
			var hasKyc = HasKycVerification(user?.UserVerification);
			var hasProvedResidence = HasProvedResidence(user?.UserVerification, rc.Tier2ResidenceRequried);
			var hasAgreement = HasTosSigned(user?.UserVerification);

			if (hasDpa && hasPersData) tier = UserTier.Tier1;

			if (hasKyc && hasProvedResidence && hasAgreement) tier = UserTier.Tier2;

			return tier;
		}

		// ---

		/// <summary>
		/// Persist user activity record
		/// </summary>
		public static DAL.Models.UserActivity CreateUserActivity(DAL.Models.Identity.User user, UserActivityType type, string comment, string ip, string agent, Locale locale) {
			return new DAL.Models.UserActivity() {
				UserId = user.Id,
				Ip = ip,
				Agent = agent.LimitLength(DAL.Models.FieldMaxLength.UserAgent),
				Type = type.ToString().ToLowerInvariant(),
				Comment = comment.LimitLength(DAL.Models.FieldMaxLength.Comment),
				TimeCreated = DateTime.UtcNow,
				Locale = locale,
			};
		}

		/// <summary>
		/// Check for pending operations
		/// </summary>
		public static async Task<bool> HasPendingBlockchainOps(IServiceProvider services, long userId) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var notTime = DateTime.UtcNow;

			return await dbContext.UserFinHistory.Where(_ =>
				_.UserId == userId &&
				_.Status == UserFinHistoryStatus.Processing &&
				(notTime - _.TimeCreated) < FinancialHistoryOvarlappingOperationsAllowedWithin
			).CountAsync() > 0;
		}

		/// <summary>
		/// User ID to long value: u0001 => 1
		/// </summary>
		public static long? ExtractId(string data) {
			if (!Common.ValidationRules.BeValidUsername(data)) {
				return null;
			}
			if (long.TryParse(data.Substring(1), out var retid)) {
				return retid;
			}
			return null;
		}

		// ---

		public sealed class UpdateUserLimitsData {

			public decimal EthDeposited { get; set; }
			public decimal EthWithdrawn { get; set; }
			public long FiatUsdDeposited { get; set; }
			public long FiatUsdWithdrawn { get; set; }
		}

		/// <summary>
		/// Change user limits
		/// </summary>
		public static async Task UpdateUserLimits(ApplicationDbContext dbContext, long userId, UpdateUserLimitsData data) {

			var nowTime = DateTime.UtcNow;
			var today = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 0, 0, 0, DateTimeKind.Utc);
			var isNew = false;

			// get from db
			var limits = await dbContext.UserLimits.FirstOrDefaultAsync(_ => _.UserId == userId && _.TimeCreated == today);

			// doesn't exist
			if (limits == null) {
				limits = new UserLimits() {
					UserId = userId,
					TimeCreated = today,
				};
				isNew = true;
			}

			limits.EthDeposited += data.EthDeposited;
			limits.EthWithdrawn += data.EthWithdrawn;
			limits.FiatDeposited += data.FiatUsdDeposited;
			limits.FiatWithdrawn += data.FiatUsdWithdrawn;

			if (isNew) {
				await dbContext.UserLimits.AddAsync(limits);
			}

			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Get user limits
		/// </summary>
		public static async Task<UpdateUserLimitsData> GetUserLimits(ApplicationDbContext dbContext, long userId) {

			var limits = await dbContext.UserLimits
				.Where(_ => _.UserId == userId)
				.GroupBy(_ => _.UserId)
				.Select(g => new UpdateUserLimitsData() {
					EthDeposited = g.Sum(_ => _.EthDeposited),
					EthWithdrawn = g.Sum(_ => _.EthWithdrawn),
					FiatUsdDeposited = g.Sum(_ => _.FiatDeposited),
					FiatUsdWithdrawn = g.Sum(_ => _.FiatWithdrawn),
				})
				.FirstOrDefaultAsync();
			;
			if (limits == null) {
				limits = new UpdateUserLimitsData();
			}
			return limits;
		}
	}
}
