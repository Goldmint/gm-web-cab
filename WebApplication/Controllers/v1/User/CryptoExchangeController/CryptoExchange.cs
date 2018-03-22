using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.CryptoExchangeModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/fiat/asset")]
	public partial class CryptoExchangeController : BaseController {

		// TODO: move to app settings constants
		private static readonly TimeSpan CryptoExchangeOperationTimeLimit = TimeSpan.FromMinutes(5);
		private static readonly TimeSpan CryptoExchangeConfirmationTimeout = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Confirm created request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("confirm")]
		[ProducesResponseType(typeof(ConfirmView), 200)]
		public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			var mutexBuilder =
				new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.CryptoExchangeConfirm, user.Id)
			;

			// get into mutex
			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					CryptoDeposit requestDeposit = null;
					CryptoDeposit requestWithdraw = null;
					DateTime? opLastTime = null;

					if (model.IsDeposit) {
						requestDeposit = await (
							from d in DbContext.CryptoDeposit
							where
								d.Id == model.RequestId &&
								d.UserId == user.Id &&
								d.Status == CryptoDepositStatus.Unconfirmed &&
								(DateTime.UtcNow - d.TimeCreated) <= CryptoExchangeConfirmationTimeout
							select d
						)
						.Include(_ => _.RefFinancialHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
						;

						opLastTime = user.UserOptions.CryptoDepositLastTime;
					}
					else {

						// requestWithdraw = ... ^

						opLastTime = user.UserOptions.CryptoWithdrawLastTime;

						throw new NotImplementedException("Withdraw is not implemented yet");
					}

					// request exists
					if (requestWithdraw == null && requestDeposit == null) {
						return APIResponse.BadRequest(nameof(model.RequestId), "Invalid request id");
					}

					// check rate
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < CryptoExchangeOperationTimeLimit) {
						return APIResponse.BadRequest(APIErrorCode.RateLimit);
					}

					// mark request for processing
					var deskTicketId = (string)null;
					if (requestDeposit != null) {
						deskTicketId = requestDeposit.DeskTicketId;

						requestDeposit.Status = CryptoDepositStatus.Confirmed;
						requestDeposit.RefFinancialHistory.Status = FinancialHistoryStatus.Manual;

						user.UserOptions.CryptoDepositLastTime = DateTime.UtcNow;
					}

					if (requestWithdraw != null) {
						deskTicketId = requestWithdraw.DeskTicketId;

						requestWithdraw.Status = CryptoDepositStatus.Confirmed;
						requestWithdraw.RefFinancialHistory.Status = FinancialHistoryStatus.Processing;

						user.UserOptions.CryptoWithdrawLastTime = DateTime.UtcNow;
					}

					await DbContext.SaveChangesAsync();

					try {
						await TicketDesk.UpdateTicket(deskTicketId, UserOpLogStatus.Pending, "Request has been confirmed by user");
					}
					catch {
					}

					return APIResponse.Success(
						new ConfirmView() { }
					);
				}

				// failed
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			});
		}

	}
}