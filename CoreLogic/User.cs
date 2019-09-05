using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL.Models;

namespace Goldmint.CoreLogic {

	public static class User {

		public static readonly TimeSpan FinancialHistoryOvarlappingOperationsAllowedWithin = TimeSpan.FromMinutes(60);

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

		// ---

		/// <summary>
		/// User's tier
		/// </summary>
		public static UserTier GetTier(DAL.Models.Identity.User user) {
			var tier = UserTier.Tier0;

			var hasAgreement = HasTosSigned(user?.UserVerification);
			var hasPersData = HasFilledPersonalData(user?.UserVerification);
			var hasKyc = HasKycVerification(user?.UserVerification);

			if (hasAgreement) tier = UserTier.Tier1;
			if (hasAgreement && hasPersData && hasKyc) tier = UserTier.Tier2;

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
				Agent = agent.Limit(DAL.Models.FieldMaxLength.UserAgent),
				Type = type.ToString().ToLowerInvariant(),
				Comment = comment.Limit(DAL.Models.FieldMaxLength.Comment),
				TimeCreated = DateTime.UtcNow,
				Locale = locale,
			};
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
