using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.CryptoCapitalModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/cryptoCapital")]
	public class CryptoCapitalController : BaseController {

		/// <summary>
		/// Setup deposit data
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Owner)]
		[HttpPost, Route("setupDeposit")]
		[ProducesResponseType(typeof(SetupDepositView), 200)]
		public async Task<APIResponse> SetupDeposit([FromBody] SetupDepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// ---

			var json = Common.Json.Stringify(model);
			if (json.Length > DAL.Models.Settings.MaxValueFieldLength) {
				return APIResponse.BadRequest("Json", "Could not save stringified table. Json is too long");
			}
			await DbContext.SaveDbSetting(DbSetting.CryptoCapitalDepositData, json);
			return APIResponse.Success(
				new SetupDepositView() { }
			);
		}
	}
}
