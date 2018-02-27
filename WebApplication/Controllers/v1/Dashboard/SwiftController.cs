using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/swift")]
	public class SwiftController : BaseController {

		/// <summary>
		/// List of SWIFT requests (both deposit/withdraw)
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<SwiftPayment, object>>>() {
				{ "id",   _ => _.Id },
				{ "type",   _ => _.Type },
				{ "status",   _ => _.Status },
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.SwiftPayment.AsQueryable();
			// exclude completed
			if (model.ExcludeCompleted) {
				query = query.Where(_ => _.Status == SwiftPaymentStatus.Pending);
			}
			// filter
			if (!string.IsNullOrWhiteSpace(model.Filter)) {
				query = query.Where(_ => _.PaymentReference.Contains(model.Filter));
			}
			query = query
				.Include(_ => _.User)
				.Include(_ => _.SupportUser)
			;

			// ---

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Type = (int)i.Type,
					Status = (int)i.Status,
					Amount = i.AmountCents / 100d,
					PaymentReference = i.PaymentReference,
					User = new ListViewItem.UserData() {
						Username = i.User.UserName,
					},
					SupportUser = i.SupportUser == null? null: new ListViewItem.SupportUserData() {
						Username = i.SupportUser.UserName,
						Comment = i.SupportComment,
					},
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
					DateCompleted = i.TimeCompleted != null? ((DateTimeOffset)i.TimeCompleted.Value).ToUnixTimeSeconds(): (long?)null,
				}
			;

			return APIResponse.Success(
				new ListView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);

		}

		/// <summary>
		/// Refuse deposit by request ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftDepositWriteAccess)]
		[HttpPost, Route("refuseDeposit")]
		[ProducesResponseType(typeof(RefuseDepositView), 200)]
		public async Task<APIResponse> RefuseDeposit([FromBody] RefuseDepositModel model) {

			var user = await GetUserFromDb();

			// get request
			var request =
				await (
					from p in DbContext.SwiftPayment
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Deposit
					select p
				)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			if (!await FinalizeRequest(user.Id, request, false, model.Comment)) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			try {
				await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"SWIFT deposit refused by support team ({user.UserName})");
			}
			catch {
			}

			return APIResponse.Success(
				new RefuseDepositView() {
				}
			);
		}

		/// <summary>
		/// Perform deposit by request ID
		/// </summary>
		/*[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftDepositWriteAccess)]
		[HttpPost, Route("acceptDeposit")]
		[ProducesResponseType(typeof(AcceptDepositView), 200)]
		public async Task<APIResponse> AcceptDeposit([FromBody] AcceptDepositModel model) {
			
			return APIResponse.Success(
				new AcceptDepositView() {
				}
			);

		}*/

		/// <summary>
		/// Refuse withdrawal request by ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftWithdrawWriteAccess)]
		[HttpPost, Route("refuseWithdraw")]
		[ProducesResponseType(typeof(RefuseWithdrawView), 200)]
		public async Task<APIResponse> RefuseWithdraw([FromBody] RefuseWithdrawModel model) {

			var user = await GetUserFromDb();

			// get request
			var request =
					await (
							from p in DbContext.SwiftPayment
							where
								p.Id == model.Id &&
								p.Type == SwiftPaymentType.Withdraw
							select p
						)
						.AsTracking()
						.FirstOrDefaultAsync()
				;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			if (!await FinalizeRequest(user.Id, request, false, model.Comment)) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			try {
				await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"SWIFT withdraw refused by support team ({user.UserName})");
			}
			catch {
			}

			return APIResponse.Success(
				new RefuseWithdrawView() {
				}
			);
		}

		// ---

		/// <summary>
		/// Set final status and support user ID, then check for ownership. Return reloaded entity or null
		/// </summary>
		private async Task<bool> FinalizeRequest(long userId, SwiftPayment request, bool success, string comment) {

			if (request.Status != SwiftPaymentStatus.Pending) {
				return false;
			}

			request.Status = success? SwiftPaymentStatus.Success: SwiftPaymentStatus.Cancelled;
			request.SupportUserId = userId;
			request.SupportComment = (comment ?? "").LimitLength(512);
			request.TimeCompleted = DateTime.UtcNow;
			await DbContext.SaveChangesAsync();

			var stamp = request.ConcurrencyStamp;
			await DbContext.Entry(request).ReloadAsync();

			return userId == (request.SupportUserId ?? 0) && request.ConcurrencyStamp == stamp;
		}
	}
}
