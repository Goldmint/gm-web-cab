using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.GoldExchangeModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/goldExchange")]
	public class GoldExchangeController : BaseController {

		/// <summary>
		/// List of requests
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> ListBuying([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<EthereumOperation, object>>>() {
				{ "date", _ => _.TimeCompleted },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.EthereumOperation.AsQueryable();
			
			// processed
			query = query.Where(_ =>
				_.Status == EthereumOperationStatus.Success &&
				(_.Type == EthereumOperationType.ContractProcessBuyRequest || _.Type == EthereumOperationType.ContractProcessSellRequest)
			);
			if (model.FilterRequestId != null) {
				query = query.Where(_ => _.RelatedRequestId != model.FilterRequestId);
			}
			if (model.PeriodStart != null) {
				query = query.Where(_ => _.TimeCompleted != null && _.TimeCompleted >= DateTimeOffset.FromUnixTimeSeconds(model.PeriodStart.Value).UtcDateTime);
			}
			if (model.PeriodEnd != null) {
				query = query.Where(_ => _.TimeCompleted != null && _.TimeCompleted <= DateTimeOffset.FromUnixTimeSeconds(model.PeriodEnd.Value).UtcDateTime);
			}

			query = query
				.Include(_ => _.User)
				.AsNoTracking()
			;

			// ---

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					RequestId = i.RelatedRequestId ?? 0,
					IsBuying = i.Type == EthereumOperationType.ContractProcessBuyRequest,
					Amount = i.GoldAmount,
					EthTxId = i.EthTransactionId,
					User = new ListViewItem.UserData() {
						Username = i.User.UserName,
					},
					DateCompleted = i.TimeCompleted != null? ((DateTimeOffset)i.TimeCompleted.Value).ToUnixTimeSeconds(): 0L,
				}
			;

			return APIResponse.Success(
				new ListView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);

		}
	}
}
