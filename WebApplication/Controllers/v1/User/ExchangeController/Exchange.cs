using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.ExchangeModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/exchange")]
	public partial class ExchangeController : BaseController {

		/// <summary>
		/// Confirm buying/selling request (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/confirm")]
		[ProducesResponseType(typeof(HWConfirmView), 200)]
		public async Task<APIResponse> HWConfirm([FromBody] HWConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var mutexBuilder =
				new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.HWOperation, user.Id)
			;

			// get into mutex
			return await mutexBuilder.LockAsync(async (ok) => {
				if (ok) {

					BuyRequest requestBuying = null;
					SellRequest requestSelling = null;
					DateTime? opLastTime = null;

					if (model.IsBuying) {
						requestBuying = await (
									from r in DbContext.BuyRequest
									where
										r.Type == ExchangeRequestType.HWRequest &&
										r.Id == model.RequestId &&
										r.UserId == user.Id &&
										r.Status == ExchangeRequestStatus.Initial
									select r
								)
								.AsTracking()
								.FirstOrDefaultAsync()
							;

						opLastTime = user.UserOptions.HotWalletBuyingLastTime;
					}
					else {
						requestSelling = await (
									from r in DbContext.SellRequest
									where
										r.Type == ExchangeRequestType.HWRequest &&
										r.Id == model.RequestId &&
										r.UserId == user.Id &&
										r.Status == ExchangeRequestStatus.Initial
									select r
								)
								.AsTracking()
								.FirstOrDefaultAsync()
							;

						opLastTime = user.UserOptions.HotWalletSellingLastTime;
					}

					// request exists
					if (requestSelling == null && requestBuying == null) {
						return APIResponse.BadRequest(nameof(model.RequestId), "Invalid request id");
					}

					// check rate
					// TODO: move to app settings constants
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < TimeSpan.FromMinutes(30)) {
						// failed
						return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
					}

					// mark request for processing
					var deskTicketId = (string) null;
					if (requestBuying != null) {
						deskTicketId = requestBuying.DeskTicketId;

						requestBuying.Status = ExchangeRequestStatus.Processing;
						requestBuying.TimeRequested = DateTime.UtcNow;
						requestBuying.TimeNextCheck = DateTime.UtcNow;
						DbContext.Update(requestBuying);

						user.UserOptions.HotWalletBuyingLastTime = DateTime.UtcNow;
					}

					if (requestSelling != null) {
						deskTicketId = requestSelling.DeskTicketId;

						requestSelling.Status = ExchangeRequestStatus.Processing;
						requestSelling.TimeRequested = DateTime.UtcNow;
						requestSelling.TimeNextCheck = DateTime.UtcNow;
						DbContext.Update(requestSelling);

						user.UserOptions.HotWalletSellingLastTime = DateTime.UtcNow;
					}

					DbContext.Update(user.UserOptions);
					await DbContext.SaveChangesAsync();

					try {
						await TicketDesk.UpdateTicket(deskTicketId, UserOpLogStatus.Pending, "Request has been confirmed by user");
					}
					catch {
					}

					return APIResponse.Success(
						new HWConfirmView() { }
					);
				}

				// failed
				return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
			});
		}

	}
}