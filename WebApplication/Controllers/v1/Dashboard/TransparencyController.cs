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
		[AreaAuthorized, AccessRights(AccessRights.TransparencyWriteAccess)]
		[HttpPost, Route("add")]
		[ProducesResponseType(typeof(AddView), 200)]
		public async Task<APIResponse> Add([FromBody] AddModel model) {
			
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var amountCents = (long)Math.Floor(model.Amount * 100);

			var user = await GetUserFromDb();

			DbContext.Transparency.Add(
				new Transparency() {
					UserId = user.Id,
					Amount = amountCents,
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
	}
}
