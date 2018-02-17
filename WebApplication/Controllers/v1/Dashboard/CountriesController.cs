using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.CountriesModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/countries")]
	public class CountriesController : BaseController {

		/// <summary>
		/// Add contry to black list
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.CountriesControl)]
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
					Code = model.Code,
					UserId = user.Id,
					Comment = model.Comment,
					TimeCreated = DateTime.UtcNow,
				}
			);
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new BanView() { }
			);
		}
	}
}
