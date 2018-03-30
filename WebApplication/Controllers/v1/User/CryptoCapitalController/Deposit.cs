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

			return APIResponse.Success(
				new DepositView() {
					CompanyName = AppConfig.Constants.CryptoCapitalData.CompanyName,
					Address = AppConfig.Constants.CryptoCapitalData.Address,
					Country = AppConfig.Constants.CryptoCapitalData.Country,
					BenAccount = AppConfig.Constants.CryptoCapitalData.BenAccount,
					Reference = AppConfig.Constants.CryptoCapitalData.Reference,
				}
			);
		}
	}
}
