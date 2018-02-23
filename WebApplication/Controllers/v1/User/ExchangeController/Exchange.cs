using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.ExchangeModels;
using Microsoft.AspNetCore.Mvc;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/exchange")]
	public partial class ExchangeController : BaseController {

		/// <summary>
		/// Confirm buying/selling request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/confirm")]
		[ProducesResponseType(typeof(HWConfirmView), 200)]
		public async Task<APIResponse> HWConfirm([FromBody] HWConfirmModel requestModel) {

			// validate
			if (BaseValidableModel.IsInvalid(requestModel, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			// get into mutex

			// get request: model.RequestId == id, user.Id == userid, status==initial, address=="HW"

			// verify last HW timestamp or fail with "time limit"

			// estimate and validate again or fail with "significant change"

			// update status 'started'
			// update ticket

			// add blockchain transaction and get txid or fail with "failed"
			// update last HW timestamp

			// update status 'pushed'
			// update ticket

			// done, check tx status in queue service

			return APIResponse.Success(
				new HWConfirmView() {
					EthTransaction = "0xDEADBEEF",
				}
			);
		}

	}
}