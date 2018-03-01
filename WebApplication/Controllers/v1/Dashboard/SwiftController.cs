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
				.Include(_ => _.User)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (!await mutex.TryEnter()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			// get limits
			var limits = await CoreLogic.UserAccount.GetCurrentFiatDepositLimit(HttpContext.RequestServices, currency, request.User);

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
						Name = request.BenName,
						Address = request.BenAddress,
						BankName = request.BenBankName,
						BankAddress = request.BenBankAddress,
						Iban = request.BenIban,
						Swift = request.BenSwift,
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
				.Include(_ => _.User)
				.AsNoTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// get lock
			var mutex = GetMutexBuilder(supportUser, request);
			if (!await mutex.TryEnter()) {
				return APIResponse.BadRequest(APIErrorCode.OwnershipLost);
			}

			// get limits
			var limits = await CoreLogic.UserAccount.GetCurrentFiatWithdrawLimit(HttpContext.RequestServices, currency, request.User);

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
						Name = request.BenName,
						Address = request.BenAddress,
						BankName = request.BenBankName,
						BankAddress = request.BenBankAddress,
						Iban = request.BenIban,
						Swift = request.BenSwift,
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

				await FinalizeRequest(supportUser.Id, request, false, model.Comment);
			
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

				await FinalizeRequest(supportUser.Id, request, false, model.Comment);

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
			var transCurrency = FiatCurrency.USD;
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			if (amountCents < AppConfig.Constants.CardPaymentData.DepositMin || amountCents > AppConfig.Constants.CardPaymentData.DepositMax) {
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
				.Include(_ => _.User).ThenInclude(_ => _.UserVerification)
				.AsTracking()
				.FirstOrDefaultAsync()
			;

			if (request == null) {
				return APIResponse.BadRequest(nameof(model.Id), "Invalid id");
			}

			// check verification
			var user = request.User;
			if (!CoreLogic.UserAccount.IsUserVerifiedL1(user)) {
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

				// fin history
				var finHistory = new DAL.Models.FinancialHistory() {
					Type = FinancialHistoryType.Deposit,
					AmountCents = amountCents,
					FeeCents = 0,
					Currency = transCurrency,
					DeskTicketId = request.DeskTicketId,
					Status = FinancialHistoryStatus.Pending,
					TimeCreated = DateTime.UtcNow,
					User = user,
					Comment = $"Deposit SWIFT payment {request.PaymentReference}",
				};
				DbContext.FinancialHistory.Add(finHistory);

				// save
				await DbContext.SaveChangesAsync();

				try {
					await TicketDesk.UpdateTicket(request.DeskTicketId, UserOpLogStatus.Pending, $"SWIFT deposit accepted by support team ({supportUser.UserName})");
				}
				catch {
				}

				await FinalizeRequest(supportUser.Id, request, false, model.Comment);

				// own scope
				using (var scopedServices = HttpContext.RequestServices.CreateScope()) {

					// try
					var queryResult = await DepositQueue.StartDepositWithSwift(
						services: scopedServices.ServiceProvider,
						request: request,
						financialHistory: finHistory
					);

					switch (queryResult.Status) {

						case FiatEnqueueStatus.Success:
							return APIResponse.Success(
								new RefuseDepositView() {
								}
							);

						case FiatEnqueueStatus.Limit:
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

		private async Task FinalizeRequest(long userId, SwiftRequest request, bool success, string comment) {
			request.Status = success? SwiftPaymentStatus.Success: SwiftPaymentStatus.Cancelled;
			request.SupportUserId = userId;
			request.SupportComment = (comment ?? "").LimitLength(512);
			request.TimeCompleted = DateTime.UtcNow;
			await DbContext.SaveChangesAsync();
		}
	}
}
