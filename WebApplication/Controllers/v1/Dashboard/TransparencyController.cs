using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.TransparencyModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/transparency")]
	public class TransparencyController : BaseController {

		/// <summary>
		/// Add new transparency record
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.TransparencyWriteAccess)]
		[HttpPost, Route("add")]
		[ProducesResponseType(typeof(AddView), 200)]
		public async Task<APIResponse> Add([FromBody] AddModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			DbContext.Transparency.Add(
				new Transparency() {
					UserId = user.Id,
					Amount = model.Amount,
					Hash = model.Hash,
					Comment = model.Comment,
					TimeCreated = DateTime.UtcNow,
				}
			);
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new AddView() { }
			);
		}

		/// <summary>
		/// Update transparency stat
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.TransparencyWriteAccess)]
		[HttpPost, Route("updateStat")]
		[ProducesResponseType(typeof(UpdateStatView), 200)]
		public async Task<APIResponse> UpdateStat([FromBody] UpdateStatModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			// ---

			DbContext.TransparencyStat.Add(
				new TransparencyStat() {
					AssetsArray = Common.Json.Stringify(model.Assets),
					BondsArray = Common.Json.Stringify(model.Bonds),
					FiatArray = Common.Json.Stringify(model.Fiat),
					GoldArray = Common.Json.Stringify(model.Gold),
					TotalOz = Common.Json.Stringify(model.TotalOz),
					TotalUsd = Common.Json.Stringify(model.TotalUsd),
					DataTimestamp = DateTimeOffset.FromUnixTimeSeconds(model.DataTimestamp).UtcDateTime,
					AuditTimestamp = DateTimeOffset.FromUnixTimeSeconds(model.AuditTimestamp).UtcDateTime,
					UserId = user.Id,
					TimeCreated = DateTime.UtcNow,
				}
			);
			await DbContext.SaveChangesAsync();

			return APIResponse.Success(
				new UpdateStatView() { }
			);
		}
	}
}
