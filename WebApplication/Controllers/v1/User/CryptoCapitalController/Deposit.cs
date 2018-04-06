using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CryptoCapitalModels;
using Microsoft.AspNetCore.Mvc;
/*
namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class CryptoCapitalController : BaseController {

		/// <summary>
		/// Deposit request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("deposit")]
		[ProducesResponseType(typeof(DepositView), 200)]
		public async Task<APIResponse> Deposit([FromBody] DepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}
			
			// ---

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var ret = new DepositView();

			// TODO: DB hit, cache response for some period
			// TODO: ser/deser via common structure

			var json = await DbContext.GetDBSetting(DbSetting.CryptoCapitalDepositData, null);
			if (json != null) {
				ret = Common.Json.Parse<DepositView>(json);
				ret.Reference = user.UserName;
			}

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.CryptoCapital,
				comment: $"Deposit request via CryptoCapital",
				ip: agent.Ip,
				agent: agent.Agent
			);

			// ---

			return APIResponse.Success(ret);
		}
	}
}
*/