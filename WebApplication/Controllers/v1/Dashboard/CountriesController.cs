using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.CountriesModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/countries")]
	public class CountriesController : BaseController {

		/// <summary>
		/// Countries black list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> BannedCountries([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.BannedCountry, object>>>() {
				{ "id",   _ => _.Id },
				{ "code", _ => _.Code },
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = from c in DbContext.BannedCountry select c;

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Code = i.Code.ToUpper(),
					Comment = i.Comment,
					Name = (new RegionInfo(i.Code)).EnglishName,
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
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
		/// Add contry to black list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.CountriesWriteAccess)]
		[HttpPost, Route("ban")]
		[ProducesResponseType(typeof(BanView), 200)]
		public async Task<APIResponse> Ban([FromBody] BanModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			DbContext.BannedCountry.Add(
				new BannedCountry() {
					Code = model.Code.ToUpper(),
					UserId = user.Id,
					Comment = model.Comment,
					TimeCreated = DateTime.UtcNow,
				}
			);

			try {
				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) {
				// actually have to catch 'duplicate'-exception here
			}

			return APIResponse.Success(
				new BanView() { }
			);
		}

		/// <summary>
		/// Remove contry from black list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.CountriesWriteAccess)]
		[HttpPost, Route("unban")]
		[ProducesResponseType(typeof(UnbanView), 200)]
		public async Task<APIResponse> Unban([FromBody] UnbanModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var country = await (
				from c in DbContext.BannedCountry
				where String.Equals(c.Code, model.Code, StringComparison.CurrentCultureIgnoreCase)
				select c
			).FirstOrDefaultAsync();

			if (country != null) {
				DbContext.BannedCountry.Remove(country);
				await DbContext.SaveChangesAsync();
			}

			return APIResponse.Success(
				new UnbanView() { }
			);
		}

	}
}
