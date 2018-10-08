using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.PromoCode;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard
{
	[Route("api/v1/dashboard/promo")]
	public class PromoController : BaseController
	{

		/// <summary>
		/// Codes list
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> List([FromBody] NoInputPagerModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<Pawn, object>>>()
			{
				{ "id",   _ => _.Id },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}
           
		    var pages = await DbContext.PromoCode
		        .OrderByDescending(_ => _.Id)
		        .Take((int)(model.Limit + model.Offset * model.Limit))
		        .PagerAsync(model.Offset, model.Limit,
		            sortExpression.GetValueOrDefault(model.Sort), model.Ascending);

		    
            return APIResponse.Success(new PromoCodesPagerView()
            {
                Items = pages.Selected.ToArray(),
                Limit = model.Limit,
                Offset = model.Offset,
                Total = pages.TotalCount,
            });
		}

	    /// <summary>
	    /// PromoCode users info
	    /// </summary>
	    [RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
	    [HttpPost, Route("info")]
	    [ProducesResponseType(typeof(object), 200)]
	    public async Task<APIResponse> PromoCodeInfo([FromBody] UsersInfo model)
	    {

	        var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<UsedPromoCodes, object>>>()
	        {
	            { "id",   _ => _.Id },
	        };

	        // validate
	        if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields))
	        {
	            return APIResponse.BadRequest(errFields);
	        }

	        var query = (
	            from r in DbContext.UsedPromoCodes
	            where
	                r.PromoCodeId == model.Id
	            select r
	        );

	        var pages = await query.PagerAsync(model.Offset, model.Limit,
	            sortExpression.GetValueOrDefault(model.Sort), model.Ascending);

	        return APIResponse.Success(new UsedCodesPagerView()
	        {
	            Items = pages.Selected.ToArray(),
	            Limit = model.Limit,
	            Offset = model.Offset,
	            Total = pages.TotalCount,
	        });
	    }

        /// <summary>
        /// Generate promo codes
        /// </summary>
        [RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.PromoCodesWriteAccess)]
		[HttpPost, Route("generate")]
		[ProducesResponseType(typeof(GenerateView), 200)]
		public async Task<APIResponse> Generate([FromBody] GenerateModel model)
		{
			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

		    EthereumToken ethereumToken;
		    decimal limit;
		    double discount;

            try
		    {
		        ethereumToken = Enum.Parse<EthereumToken>(model.Currency, true);
		        limit = decimal.Parse(model.Limit, CultureInfo.InvariantCulture);            
		        discount = double.Parse(model.DiscountValue, CultureInfo.InvariantCulture);  

            }
		    catch (Exception)
            {
		        return APIResponse.BadRequest(APIErrorCode.InvalidParameter);
            }

            var chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
			var makeCode = new Func<string>(() => 
			{
                using (var alg = System.Security.Cryptography.SHA1.Create())
                {
                    var hash = alg.ComputeHash(Guid.NewGuid().ToByteArray());
                    var result = new char[hash.Length / 2];
                    for (var i = 0; i < result.Length; i++)
                    {
                        var v = BitConverter.ToUInt16(hash, i * 2);
                        result[i] = chars[v % chars.Length];
                    }
                    return (new string(result)).Insert(5, "-");
                }
			});

			var now = DateTime.UtcNow;
			var until = now.AddDays(model.ValidForDays);
            
            var list = new List<Pawn>();
			for (var i = 0; i < model.Count; ++i)
			{
				list.Add(
					new Pawn()
					{
						Code = makeCode(),
					    Currency = ethereumToken,
					    Limit = limit,
                        DiscountValue = discount,
					    UsageType = model.UsageType,
                        TimeCreated = now,
						TimeExpires = until,
					}
				);
			}

			DbContext.AddRange(list);

		    await DbContext.SaveChangesAsync();
			
			return APIResponse.Success(
				new GenerateView()
				{
					Codes = list.Select(_ => _.Code).ToArray(),
				}
			);
		}
	}
}
