using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.Dashboard.PromoModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Models.API;
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
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<PromoCode, object>>>()
			{
				{ "id",   _ => _.Id },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.PromoCode
				.Include(_ => _.User)
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(model.Filter)) {
				query = query.Where(_ => _.Code.Contains(model.Filter) || (_.User != null && _.User.UserName.Contains(model.Filter)));
			}
			if (model.FilterUsed != null)
			{
			    query = model.FilterUsed.Value 
			        ? query.Where(_ => _.UserId != null) 
			        : query.Where(_ => _.UserId == null);
			}

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem()
				{
					Id = i.Id,
					Username = i.User?.UserName,
					Code = i.Code,
				    Currency = i.Currency,
				    Limit = i.Limit,
                    DiscountValue = i.DiscountValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
					TimeCreated = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
					TimeExpires = ((DateTimeOffset)i.TimeExpires).ToUnixTimeSeconds(),
					TimeUsed = i.TimeUsed != null? ((DateTimeOffset)i.TimeUsed.Value).ToUnixTimeSeconds(): (long?)(null),
				}
			;

			return APIResponse.Success(
				new ListView()
				{
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
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
		        limit = decimal.Parse(model.Limit, CultureInfo.InvariantCulture);            //less zero ?
		        discount = double.Parse(model.DiscountValue, CultureInfo.InvariantCulture);  //less zero ?  

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
            
            var list = new List<PromoCode>();
			for (var i = 0; i < model.Count; ++i)
			{
				list.Add(
					new PromoCode()
					{
						Code = makeCode(),
					    Currency = ethereumToken,
					    Limit = limit,
                        DiscountValue = discount,
						TimeCreated = now,
						TimeExpires = until,
						TimeUsed = null,
						UserId = null,
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
