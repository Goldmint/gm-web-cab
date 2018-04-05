using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.DAL.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/swift")]
	public class SwiftController : BaseController {

		/// <summary>
		/// List of SWIFT requests (both deposit/withdraw)
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpPost, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List([FromBody] ListModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<SwiftRequest, object>>>() {
				{ "id",   _ => _.Id },
				{ "type",   _ => _.Type },
				{ "status",   _ => _.Status },
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = DbContext.SwiftRequest.AsQueryable();
			// exclude completed
			if (model.ExcludeCompleted) {
				query = query.Where(_ => _.Status == SwiftPaymentStatus.Pending);
			}
			// filter by type
			if (model.Type == 1 || model.Type == 2) {
				query = query.Where(_ => _.Type == (model.Type == 1? SwiftPaymentType.Deposit: SwiftPaymentType.Withdraw));
			}
			// filter
			if (!string.IsNullOrWhiteSpace(model.Filter)) {
				query = query.Where(_ => _.PaymentReference.Contains(model.Filter));
			}
			query = query
				.Include(_ => _.User)
				.Include(_ => _.SupportUser)
			;

			// ---

			var page = await query.PagerAsync(model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ListViewItem() {
					Id = i.Id,
					Type = (int)i.Type,
					Status = (int)i.Status,
					Amount = i.AmountCents / 100d,
					PaymentReference = i.PaymentReference,
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

		// ---

		/// <summary>
		/// Acquire lock on request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftDepositWriteAccess)]
		[HttpPost, Route("lockDeposit")]
		[ProducesResponseType(typeof(LockDepositView), 200)]
		public async Task<APIResponse> LockDeposit([FromBody] LockModel model) {

			var supportUser = await GetUserFromDb();
			var currency = FiatCurrency.USD;

			// get request
			var request = await (
					from p in DbContext.SwiftRequest
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Deposit
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

			// get limits
			var userTier = CoreLogic.User.GetTier(request.User);
			var limits = await CoreLogic.User.GetCurrentFiatDepositLimit(HttpContext.RequestServices, currency, request.User.Id, userTier);

			return APIResponse.Success(
				new LockDepositView() {
					User = new LockDepositView.UserData() {
						Username = request.User.UserName,
						FiatLimits = new LockDepositView.UserData.PeriodLimitItem() {
							Minimal = limits.Minimal,
							Day = limits.Day,
							Month = limits.Month,
						}
					},
					BankInfo = new LockDepositView.BankInfoData() {
						Name = request.Holder,
						Address = request.HolderAddress,
						BankName = request.Bank,
						BankAddress = request.Details,
						Iban = request.Iban,
						Swift = request.Bic,
					},
				}
			);
		}

		/// <summary>
		/// Acquire lock on request
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftWithdrawWriteAccess)]
		[HttpPost, Route("lockWithdraw")]
		[ProducesResponseType(typeof(LockWithdrawView), 200)]
		public async Task<APIResponse> LockWithdraw([FromBody] LockModel model) {

			var supportUser = await GetUserFromDb();
			var currency = FiatCurrency.USD;

			// get request
			var request = await (
					from p in DbContext.SwiftRequest
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Withdraw
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

			// get limits
			var userTier = CoreLogic.User.GetTier(request.User);
			var limits = await CoreLogic.User.GetCurrentFiatWithdrawLimit(HttpContext.RequestServices, currency, request.User.Id, userTier);

			return APIResponse.Success(
				new LockWithdrawView() {
					User = new LockDepositView.UserData() {
						Username = request.User.UserName,
						FiatLimits = new LockDepositView.UserData.PeriodLimitItem() {
							Minimal = limits.Minimal,
							Day = limits.Day,
							Month = limits.Month,
						}
					},
					BankInfo = new LockDepositView.BankInfoData() {
						Name = request.Holder,
						Address = request.HolderAddress,
						BankName = request.Bank,
						BankAddress = request.Details,
						Iban = request.Iban,
						Swift = request.Bic,
					},
				}
			);
		}

		// ---

		/// <summary>
		/// Refuse deposit by request ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftDepositWriteAccess)]
		[HttpPost, Route("refuseDeposit")]
		[ProducesResponseType(typeof(RefuseDepositView), 200)]
		public async Task<APIResponse> RefuseDeposit([FromBody] RefuseDepositModel model) {

			var supportUser = await GetUserFromDb();

			// get request
			var request = await (
					from p in DbContext.SwiftRequest
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Deposit
					select p
				)
				.Include(_ => _.RefFinancialHistory)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (request.Status != SwiftPaymentStatus.Pending || !await mutex.TryLeave()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			return await mutex.CriticalSection(async (ok) => {

				if (!ok) {
					return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
				}

				request.RefFinancialHistory.Status = FinancialHistoryStatus.Failed;
				MarkRequestFinalized(supportUser.Id, request, false, model.Comment);				
				await DbContext.SaveChangesAsync();

				try {
					await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"SWIFT deposit refused by support team ({supportUser.UserName})");
				}
				catch {
				}

				return APIResponse.Success(
					new RefuseDepositView() {
					}
				);
			});
		}

		/// <summary>
		/// Refuse withdrawal request by ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftWithdrawWriteAccess)]
		[HttpPost, Route("refuseWithdraw")]
		[ProducesResponseType(typeof(RefuseWithdrawView), 200)]
		public async Task<APIResponse> RefuseWithdraw([FromBody] RefuseWithdrawModel model) {

			var supportUser = await GetUserFromDb();

			// get request
			var request = await (
					from p in DbContext.SwiftRequest
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Withdraw
					select p
				)
				.Include(_ => _.RefFinancialHistory)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (request.Status != SwiftPaymentStatus.Pending || !await mutex.TryLeave()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			return await mutex.CriticalSection(async (ok) => {

				if (!ok) {
					return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
				}

				request.RefFinancialHistory.Status = FinancialHistoryStatus.Failed;
				MarkRequestFinalized(supportUser.Id, request, false, model.Comment);
				await DbContext.SaveChangesAsync();

				try {
					await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Failed, $"SWIFT withdraw refused by support team ({supportUser.UserName})");
				}
				catch {
				}

				return APIResponse.Success(
					new RefuseWithdrawView() {
					}
				);
			});
		}

		// ---

		/// <summary>
		/// Accept deposit by request ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.SwiftDepositWriteAccess)]
		[HttpPost, Route("acceptDeposit")]
		[ProducesResponseType(typeof(AcceptDepositView), 200)]
		public async Task<APIResponse> AcceptDeposit([FromBody] AcceptDepositModel model) {

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			if (amountCents < AppConfig.Constants.SwiftData.DepositMin || (amountCents > AppConfig.Constants.SwiftData.DepositMax) && AppConfig.Constants.SwiftData.DepositMax != 0) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var supportUser = await GetUserFromDb();

			// get request
			var request = await (
					from p in DbContext.SwiftRequest
					where
						p.Id == model.Id &&
						p.Type == SwiftPaymentType.Deposit
					select p
				)
				.Include(_ => _.User).ThenInclude(_ => _.UserOptions)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastKycTicket)
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification).ThenInclude(_ => _.LastAgreement)
				.Include(_ => _.RefFinancialHistory)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// check verification
			var user = request.User;
			var userTier = CoreLogic.User.GetTier(user);
			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (request.Status != SwiftPaymentStatus.Pending || !await mutex.TryLeave()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			return await mutex.CriticalSection(async (ok) => {

				if (!ok) {
					return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
				}

				try {
					await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"SWIFT deposit accepted by support team ({supportUser.UserName})");
				}
				catch {
				}

				// own scope
				using (var scopedServices = HttpContext.RequestServices.CreateScope()) {

					// try
					var queryResult = await DepositQueue.StartDepositWithSwift(
						services: scopedServices.ServiceProvider,
						userId: request.User.Id,
						userTier: userTier,
						request: request,
						financialHistoryId: request.Id
					);

					switch (queryResult.Status) {

						case FiatEnqueueResult.Success:

							request.RefFinancialHistory.Status = FinancialHistoryStatus.Completed;
							MarkRequestFinalized(supportUser.Id, request, false, model.Comment);
							await DbContext.SaveChangesAsync();

							return APIResponse.Success(
								new RefuseDepositView() {
								}
							);

						case FiatEnqueueResult.Limit:
							return APIResponse.BadRequest(APIErrorCode.AccountDepositLimit);

						default:
							return APIResponse.GeneralInternalFailure();
					}
				}
			});
		}

		// ---

		private MutexBuilder GetMutexBuilder(DAL.Models.Identity.User user, SwiftRequest request) {
			return new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.SupportSwiftRequestProc, request.Id)
				.LockerUser(user.Id)
				.Timeout(TimeSpan.FromMinutes(30))
			;
		}

		private void MarkRequestFinalized(long userId, SwiftRequest request, bool success, string comment) {
			request.Status = success? SwiftPaymentStatus.Success: SwiftPaymentStatus.Cancelled;
			request.SupportUserId = userId;
			request.SupportComment = (comment ?? "").LimitLength(512);
			request.TimeCompleted = DateTime.UtcNow;
		}
	}
}
