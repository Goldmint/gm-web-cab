using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.Dashboard.UsersModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Models.API;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/users")]
	public class UsersController : BaseController {

		/// <summary>
		/// Users list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.Identity.User, object>>>() {
				{ "id",   _ => _.Id },
				{ "username", _ => _.UserName },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = (
				string.IsNullOrWhiteSpace(model.Filter)
					? from a in DbContext.Users select a
					: from a in DbContext.Users where 
						a.UserName.Contains(model.Filter) ||
						(a.UserVerification != null && (a.UserVerification.FirstName + " " + a.UserVerification.LastName).Contains(model.Filter))
					  select a
			)
				.Include(user => user.UserOptions)
				.Include(user => user.UserVerification)
			;

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Username = i.UserName,
					Name = string.Format(
						"{0} {1}",
						i.UserVerification?.FirstName ?? "",
						i.UserVerification?.LastName ?? ""
					).Trim(' '),
					TimeRegistered = ((DateTimeOffset)i.TimeRegistered).ToUnixTimeSeconds(),
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
		/// User account info
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("account")]
		[ProducesResponseType(typeof(AccountView), 200)]
		public async Task<APIResponse> Account([FromBody] AccountModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var account = await (
				from a in DbContext.Users
				where a.Id == model.Id
				select a
			)
				.AsNoTracking()
					.Include(_ => _.UserVerification)
				.FirstOrDefaultAsync()
			;

			if (account == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// ---

			var properties = new List<AccountView.PropertiesItem>() {
				new AccountView.PropertiesItem(){ N = "ID", V = account.Id.ToString() },
				new AccountView.PropertiesItem(){ N = "Username", V = account.UserName ?? "-" },
				new AccountView.PropertiesItem(){ N = "Email", V = account.Email ?? "-" },
				new AccountView.PropertiesItem(){ N = "Registered", V = account.TimeRegistered.ToString("yyyy MMMM dd") },
			};

			var accessRights = new List<AccountView.AccessRightsItem>();
			foreach (var v in (AccessRights[]) Enum.GetValues(typeof(AccessRights))) {
				var mask = (long) v;

				accessRights.Add(
					new AccountView.AccessRightsItem() {
						N = v.ToString(),
						C = (account.AccessRights & mask) == mask,
						M = mask,
					}
				);
			}

			return APIResponse.Success(
				new AccountView() {
					Properties = properties.ToArray(),
					AccessRights = accessRights.ToArray(),
				}
			);
		}

		/// <summary>
		/// User's oplog
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("oplog")]
		[ProducesResponseType(typeof(OplogView), 200)]
		public async Task<APIResponse> Oplog([FromBody] OplogModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<UserOpLog, object>>>() {
				{ "date",   _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// search in children
			var filteredRoots = new List<long>();
			if (!string.IsNullOrWhiteSpace(model.Filter)) {
				filteredRoots = await (
					from o in DbContext.UserOpLog
					where o.UserId == model.Id && o.RefId != null && o.Message.Contains(model.Filter)
					group o by o.RefId.Value into g
					select g.Key
				)
					.ToListAsync()
				;
			}

			var query = string.IsNullOrWhiteSpace(model.Filter)
				// no filter
				? from o in DbContext.UserOpLog where o.UserId == model.Id && o.RefId == null select o
				// filter, including children
				: from o in DbContext.UserOpLog where o.UserId == model.Id && o.RefId == null && (o.Message.Contains(model.Filter) || filteredRoots.Contains(o.Id)) select o
			;

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			// find all children
			var steps = new List<UserOpLog>();
			if (page.Selected.Length > 0) {
				var ids = (from o in page.Selected select o.Id).ToList();
				steps = await (
					from s in DbContext.UserOpLog where s.RefId != null && ids.Contains(s.RefId ?? 0) select s
				)
					.AsNoTracking()
					.ToListAsync()
				;
			}

			// ---

			var list =
				from o in page.Selected
				select new OplogViewItem() {
					Id = o.Id,
					Message = o.Message,
					Status = (int)o.Status,
					Date = ((DateTimeOffset)o.TimeCreated).ToUnixTimeSeconds(),
					Steps = (from s in steps where s.RefId == o.Id select new OplogViewItem() {
						Id = s.Id,
						Message = s.Message,
						Status = (int)s.Status,
						Date = ((DateTimeOffset)s.TimeCreated).ToUnixTimeSeconds(),
						Steps = null,
					}).ToArray(),
				}
			;

			return APIResponse.Success(
				new OplogView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}

		/// <summary>
		/// Set access rights
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Owner)]
		[HttpPost, Route("rights")]
		[ProducesResponseType(typeof(RightsView), 200)]
		public async Task<APIResponse> Rights([FromBody] RightsModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var account = await (
					from a in DbContext.Users
					where a.Id == model.Id
					select a
				)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (account == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			account.AccessRights = model.Mask;
			DbContext.Update(account);
			await DbContext.SaveChangesAsync();
			
			return APIResponse.Success(
				new RightsView() {
				}
			);
		}
	}
}
