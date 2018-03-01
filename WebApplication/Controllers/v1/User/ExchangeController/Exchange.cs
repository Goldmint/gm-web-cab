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
using System.Numerics;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/exchange")]
	public partial class ExchangeController : BaseController {

		// TODO: move to app settings constants
		private static readonly TimeSpan HWOperationTimeLimit = TimeSpan.FromMinutes(30);

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
			return await mutexBuilder.CriticalSection(async (ok) => {
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
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
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

						user.UserOptions.HotWalletBuyingLastTime = DateTime.UtcNow;
					}

					if (requestSelling != null) {
						deskTicketId = requestSelling.DeskTicketId;

						requestSelling.Status = ExchangeRequestStatus.Processing;
						requestSelling.TimeRequested = DateTime.UtcNow;
						requestSelling.TimeNextCheck = DateTime.UtcNow;

						user.UserOptions.HotWalletSellingLastTime = DateTime.UtcNow;
					}

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

		/// <summary>
		/// Transferring request of GOLD to eth address (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/transfer")]
		[ProducesResponseType(typeof(HWTransferView), 200)]
		public async Task<APIResponse> HWTransfer([FromBody] HWTransferModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// ---

			// check pending operations
			if (await CoreLogic.UserAccount.HasPendingBlockchainOps(HttpContext.RequestServices, user)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var amountWei = BigInteger.Zero;
			if (!BigInteger.TryParse(model.Amount, out amountWei)) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}
			var goldBalance = await EthereumObserver.GetUserGoldBalance(user.UserName);

			if (amountWei < 1 || amountWei > goldBalance || amountWei.ToString().Length > 64) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var mutexBuilder =
				new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.HWOperation, user.Id)
			;

			// get into mutex
			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var opLastTime = user.UserOptions.HotWalletTransferLastTime;

					// check rate
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
						// failed
						return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
					}

					var ticket = await TicketDesk.NewGoldTransfer(user, model.EthAddress, amountWei);

					// history
					var finHistory = new DAL.Models.FinancialHistory() {
						Type = FinancialHistoryType.HWTransfer,
						AmountCents = (long)(CoreLogic.Finance.Tokens.GoldToken.FromWei(amountWei) * 100),
						FeeCents = 0,
						DeskTicketId = ticket,
						Status = FinancialHistoryStatus.Pending,
						TimeCreated = DateTime.UtcNow,
						User = user,
						Comment = "" // see below
					};

					// save
					DbContext.FinancialHistory.Add(finHistory);
					await DbContext.SaveChangesAsync();

					// request
					var request = new TransferRequest() {
						User = user,
						Status = ExchangeRequestStatus.Processing,
						DestinationAddress = model.EthAddress,
						AmountWei = amountWei.ToString(),
						DeskTicketId = ticket,
						RefFinancialHistory = finHistory,
						TimeCreated = DateTime.UtcNow,
						TimeNextCheck = DateTime.UtcNow,
					};

					// save
					DbContext.TransferRequest.Add(request);
					await DbContext.SaveChangesAsync();

					// update comment
					finHistory.Comment = $"Transfer order #{request.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(amountWei)} GOLD from hot wallet to {TextFormatter.MaskEthereumAddress(model.EthAddress)}";
					await DbContext.SaveChangesAsync();

					try {
						await TicketDesk.UpdateTicket(ticket, UserOpLogStatus.Pending, $"Transfer request ID is #{request.Id}");
					}
					catch {
					}

					// activity
					await CoreLogic.UserAccount.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.Exchange,
						comment: $"GOLD transfer request #{request.Id} ({ CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(amountWei) } oz) from HW to {Common.TextFormatter.MaskEthereumAddress(model.EthAddress)} initiated",
						ip: agent.Ip,
						agent: agent.Agent
					);

					user.UserOptions.HotWalletTransferLastTime = DateTime.UtcNow;
					await DbContext.SaveChangesAsync();

					return APIResponse.Success(
						new HWTransferView() { }
					);
				}

				// failed
				return APIResponse.BadRequest(APIErrorCode.AccountHWOperationLimit);
			});
		}

	}
}