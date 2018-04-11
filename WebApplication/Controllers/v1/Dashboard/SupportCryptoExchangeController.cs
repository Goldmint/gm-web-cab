using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.SupportCryptoExchangeModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/supportCryptoExchange")]
	public class SupportCryptoExchangeController : BaseController {

		/// <summary>
		/// List of requests
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("listBuying")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> ListBuying([FromBody] ListBuyingModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<BuyGoldCryptoSupportRequest, object>>>() {
				{ "id",   _ => _.Id },
				{ "status",   _ => _.Status },
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.BuyGoldCryptoSupportRequest.AsQueryable();
			if (model.ExcludeCompleted) {
				query = query.Where(_ => _.Status == SupportRequestStatus.Pending);
			}
			query = query
				.Include(_ => _.User)
				.Include(_ => _.SupportUser)
				.Include(_ => _.BuyGoldRequest)
				.AsNoTracking()
			;

			// ---

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Status = (int)i.Status,
					Amount = i.AmountWei,
					ExchangeCurrency = i.BuyGoldRequest.ExchangeCurrency.ToString().ToUpper(),
					CryptoAsset = "ETH",
					User = new ListViewItem.UserData() {
						Username = i.User.UserName,
					},
					SupportUser = i.SupportUser == null? null: new ListViewItem.SupportUserData() {
						Username = i.SupportUser.UserName,
						Comment = i.SupportComment,
					},
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
					DateCompleted = i.TimeCompleted != null? ((DateTimeOffset)i.TimeCompleted.Value).ToUnixTimeSeconds(): (long?)null,
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

		/// <summary>
		/// List of requests
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("listSelling")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> ListSelling([FromBody] ListSellingModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<SellGoldCryptoSupportRequest, object>>>() {
				{ "id",   _ => _.Id },
				{ "status",   _ => _.Status },
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.SellGoldCryptoSupportRequest.AsQueryable();
			if (model.ExcludeCompleted) {
				query = query.Where(_ => _.Status == SupportRequestStatus.Pending);
			}
			query = query
				.Include(_ => _.User)
				.Include(_ => _.SupportUser)
				.Include(_ => _.SellGoldRequest)
				.AsNoTracking()
			;

			// ---

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Status = (int)i.Status,
					Amount = i.AmountWei,
					ExchangeCurrency = i.SellGoldRequest.ExchangeCurrency.ToString().ToUpper(),
					CryptoAsset = "ETH",
					User = new ListViewItem.UserData() {
						Username = i.User.UserName,
					},
					SupportUser = i.SupportUser == null ? null : new ListViewItem.SupportUserData() {
						Username = i.SupportUser.UserName,
						Comment = i.SupportComment,
					},
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
					DateCompleted = i.TimeCompleted != null ? ((DateTimeOffset)i.TimeCompleted.Value).ToUnixTimeSeconds() : (long?)null,
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

		// ---

		/// <summary>
		/// Acquire lock on request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.BuyRequestsWriteAccess)]
		[HttpPost, Route("lockBuying")]
		[ProducesResponseType(typeof(LockBuyingView), 200)]
		public async Task<APIResponse> LockBuying([FromBody] LockModel model) {

			var supportUser = await GetUserFromDb();

			// get request
			var request = await (
					from p in DbContext.BuyGoldCryptoSupportRequest
					where
						p.Id == model.Id
					select p
				)
				.Include(_ => _.User).ThenInclude(_ => _.UserOptions)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastKycTicket)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastAgreement)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (request?.User == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (!await mutex.TryEnter()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			return APIResponse.Success(
				new LockBuyingView() {
					User = new LockBuyingView.UserData() {
						Username = request.User.UserName,
					},
				}
			);
		}

		/// <summary>
		/// Acquire lock on request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SellRequestsWriteAccess)]
		[HttpPost, Route("lockSelling")]
		[ProducesResponseType(typeof(LockSellingView), 200)]
		public async Task<APIResponse> LockSelling([FromBody] LockModel model) {

			var supportUser = await GetUserFromDb();

			// get request
			var request = await (
					from p in DbContext.SellGoldCryptoSupportRequest
					where
						p.Id == model.Id
					select p
				)
				.Include(_ => _.User).ThenInclude(_ => _.UserOptions)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastKycTicket)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastAgreement)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (request?.User == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (!await mutex.TryEnter()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			return APIResponse.Success(
				new LockSellingView() {
					User = new LockSellingView.UserData() {
						Username = request.User.UserName,
					},
				}
			);
		}

		// ---

		[NonAction]
		private MutexBuilder GetMutexBuilder(DAL.Models.Identity.User user, BuyGoldCryptoSupportRequest request) {
			return new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.SupportBuyRequestProc, request.Id)
				.LockerUser(user.Id)
				.Timeout(TimeSpan.FromMinutes(30))
			;
		}

		[NonAction]
		private MutexBuilder GetMutexBuilder(DAL.Models.Identity.User user, SellGoldCryptoSupportRequest request) {
			return new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.SupportSellRequestProc, request.Id)
				.LockerUser(user.Id)
				.Timeout(TimeSpan.FromMinutes(30))
			;
		}
	}
}
