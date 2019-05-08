using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.UserModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class UserController : BaseController {

		/// <summary>
		/// Profile info
		/// </summary>
		[RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("profile")]
		[ProducesResponseType(typeof(ProfileView), 200)]
		public async Task<APIResponse> Profile() {

		    var rcfg = RuntimeConfigHolder.Clone();
            var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user, rcfg);

			// user challenges
			var challenges = new List<string>();
			if (!user.UserOptions.InitialTfaQuest && !user.TwoFactorEnabled) challenges.Add("2fa");

			return APIResponse.Success(
				new ProfileView() {
					Id = user.UserName,
					Name = CoreLogic.User.HasFilledPersonalData(user.UserVerification) ? (user.UserVerification.FirstName + " " + user.UserVerification.LastName).Trim() : user.UserName,
					Email = user.Email ?? "",
					DpaSigned = user.UserOptions.DpaDocument?.IsSigned ?? false,
					TfaEnabled = user.TwoFactorEnabled,
					HasExtraRights = (user.AccessRights & (long)AccessRights.ClientExtraAccess) == (long)AccessRights.ClientExtraAccess,
					VerifiedL0 = userTier >= UserTier.Tier1,
					VerifiedL1 = userTier >= UserTier.Tier2,
					Challenges = challenges.ToArray(),
				}
			);
		}

		/// <summary>
		/// Account info
		/// </summary>
		[RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("account")]
		[ProducesResponseType(typeof(ProfileView), 200)]
		public async Task<APIResponse> Account() {
            var user = await GetUserFromDb();

			var gold = user.UserSumusWallet.BalanceGold.ToSumus(); 
			var mnt = user.UserSumusWallet.BalanceMnt.ToSumus(); 

			return APIResponse.Success(
				new AccountView() {
					SumusGold = gold.ToString(),
					SumusMnt = mnt.ToString(),
					SumusWallet = user.UserSumusWallet.PublicKey,
				}
			);
		}

		/// <summary>
		/// User activity
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("activity")]
		[ProducesResponseType(typeof(ActivityView), 200)]
		public async Task<APIResponse> Activity([FromBody] ActivityModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.UserActivity, object>>>() {
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			var query = (
				from a in DbContext.UserActivity
				where a.UserId == user.Id
				select a
			);

			var page = await DalExtensions.PagerAsync(query, model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ActivityViewItem() {
					Type = i.Type.ToLower(),
					Comment = i.Comment,
					Ip = i.Ip,
					Agent = i.Agent,
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
				}
			;

			return APIResponse.Success(
				new ActivityView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}

		/// <summary>
		/// Fiat history
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("history")]
		[ProducesResponseType(typeof(FiatHistoryView), 200)]
		public async Task<APIResponse> FiatHistory([FromBody] FiatHistoryModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.UserFinHistory, object>>>() {
				{ "date",   _ => _.TimeCreated },
				{ "type",   _ => _.Type },
				{ "status", _ => _.Status }
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			var query = (
				from h in DbContext.UserFinHistory
				where 
					h.UserId == user.Id &&
					(
						h.Status == UserFinHistoryStatus.Manual || 
						h.Status == UserFinHistoryStatus.Processing ||
						h.Status == UserFinHistoryStatus.Completed ||
						h.Status == UserFinHistoryStatus.Failed
					)
				select h
			);

			var page = await query.PagerAsync(
				model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var nowTime = DateTime.UtcNow;

			var list =
				from i in page.Selected
				select new FiatHistoryViewItem() {
					Type = i.Type.ToString().ToLower(),
					Status = (
						i.Status == UserFinHistoryStatus.Completed
						? 2 // success
						: i.Status == UserFinHistoryStatus.Failed || (i.TimeExpires != null && i.TimeExpires <= nowTime)
							? 3 // cancelled/failed
							: 1 // pending
					),
					Comment = i.Comment,
					Src = i.Source,
					SrcAmount = i.SourceAmount,
					Dst = i.Destination,
					DstAmount = i.DestinationAmount,
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
					EthTxId = i.RelEthTransactionId,
				}
			;

			return APIResponse.Success(
				new FiatHistoryView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}
	}
}