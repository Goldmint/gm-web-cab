using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

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
				data?.LastAgreement != null &&
				data.LastAgreement.Type == SignedDocumentType.Tos &&
				data.LastAgreement.TimeCompleted != null &&
				data.LastAgreement.IsSigned
			;
		}

		public static bool HasProvedResidence(DAL.Models.UserVerification data) {
			return
				data?.ProvedResidence ?? false
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
			var hasKyc = HasKycVerification(user?.UserVerification);
			var hasProvedResidence = HasProvedResidence(user?.UserVerification);
			var hasAgreement = HasTosSigned(user?.UserVerification);

			if (hasDpa && hasPersData) tier = UserTier.Tier1;

			if (hasKyc && hasProvedResidence && hasAgreement) tier = UserTier.Tier2;

			return tier;
		}

		// ---

		/// <summary>
		/// Persist user activity record
		/// </summary>
		public static async Task SaveActivity(IServiceProvider services, DAL.Models.Identity.User user, UserActivityType type, string comment, string ip, string agent) {

			var dbContext = services.GetRequiredService<ApplicationDbContext>();

			var activity = new DAL.Models.UserActivity() {
				UserId = user.Id,
				Ip = ip,
				Agent = agent.LimitLength(DAL.Models.FieldMaxLength.UserAgent),
				Type = type.ToString().ToLowerInvariant(),
				Comment = comment.LimitLength(DAL.Models.FieldMaxLength.Comment),
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

			return await dbContext.UserFinHistory.Where(_ =>
				_.UserId == userId &&
				_.Status == UserFinHistoryStatus.Processing &&
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

	}
}
