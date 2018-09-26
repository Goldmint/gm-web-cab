using Goldmint.Common;
using Goldmint.WebApplication.Core.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.DAL.CustodyBotModels;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard
{
	[Route("api/v1/dashboard/custody")]
	public class CustodyBotController : BaseController
	{
        /// <summary>
        /// Get bots and merchants list
        /// </summary>
        //[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Owner)]
		[HttpPost, Route("bot_list")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> BotList([FromBody] NoInputPagerModel model)
        {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<BotsInfo, object>>>()
			{
			    { "id",   _ => _.Id },
            };

			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

            var query = (
                    from r in CBotDbContext.Clients
                    where
                        r.Role == ClientRole.RoleOrgBot ||
                        r.Role == ClientRole.RoleOrgMerchant
                    select r)
                .Select(x => new BotsInfo
                {
                    Id = x.Id,
                    Name = x.Name,
                    SumusAddress = x.SumusAddress,
                });
                

            var pages = await query.PagerAsync(model.Offset, model.Limit,
                sortExpression.GetValueOrDefault(model.Sort), model.Ascending);

            return APIResponse.Success(
                new BotsPagerView()
            {
                Items = pages.Selected.ToArray(),
                Limit = model.Limit,
                Offset = model.Offset,
                Total = pages.TotalCount,
            });
		}


		/// <summary>
		/// get custody pawns
		/// </summary>
		//[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.PromoCodesWriteAccess)]
		[HttpPost, Route("pawns")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> Pawns([FromBody] NoInputPagerModel model)
		{
		    var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<Custodies, object>>>()
		    {
		        { "id",   _ => _.Id },
		    };

		    if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields))
		    {
		        return APIResponse.BadRequest(errFields);
		    }

		    var pages = await CBotDbContext.Custodies
		        .OrderByDescending(_ => _.Id)
		        .Take((int)(model.Limit + model.Offset * model.Limit))
		        .PagerAsync(model.Offset, model.Limit,
		            sortExpression.GetValueOrDefault(model.Sort), model.Ascending);


            return APIResponse.Success(
                new PawnsPagerView()
            {
                Items = pages.Selected.ToArray(),
                Limit = model.Limit,
                Offset = model.Offset,
                Total = pages.TotalCount,
            });
		}
	}
}
