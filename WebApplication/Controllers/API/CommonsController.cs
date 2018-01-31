using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Models.API.CommonsModels;
using Goldmint.WebApplication.Core.Response;

namespace Goldmint.WebApplication.Controllers.API {

	[Route("api/v1/commons")]
	public class CommonsController : BaseController {

		/// <summary>
		/// Price per gold ounce
		/// </summary>
		[AreaAnonymous]
		[HttpGet, Route("goldRate")]
		[ProducesResponseType(typeof(GoldRateView), 200)]
		public async Task<APIResponse> GoldRate() {
			return APIResponse.Success(
				new GoldRateView() {
					Rate = await GoldRateProvider.GetGoldRate(Common.FiatCurrency.USD) / 100d,
				}
			);
		}

	}
}