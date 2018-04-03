using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.FiatFeesModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/fees")]
	public class FeesController : BaseController {

		/// <summary>
		/// Update fees table
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.TransparencyWriteAccess)]
		[HttpPost, Route("update")]
		[ProducesResponseType(typeof(UpdateView), 200)]
		public async Task<APIResponse> Update([FromBody] UpdateModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// ---

			var json = Common.Json.Stringify(model);
			if (json.Length > DAL.Models.Settings.MaxValueFieldLength) {
				return APIResponse.BadRequest("Json", "Could not save stringified table. Json is too long");
			}
			await DbContext.SaveDbSetting(DbSetting.FeesTable, json);
			return APIResponse.Success(
				new UpdateView() { }
			);
		}
	}
}
