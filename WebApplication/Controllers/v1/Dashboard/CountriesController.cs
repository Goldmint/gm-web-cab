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

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/countries")]
	public class CountriesController : BaseController {

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
					Code = model.Code.ToLower(),
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
