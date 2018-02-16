using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.UserModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class UserController : BaseController {

		/// <summary>
		/// Fiat and gold balance on this user account
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("balance")]
		[ProducesResponseType(typeof(BalanceView), 200)]
		public async Task<APIResponse> Balance([FromBody] BalanceModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			return APIResponse.Success(
				new BalanceView() {
					Usd = await EthereumObserver.GetUserFiatBalance(user.UserName, FiatCurrency.USD) / 100d,
					Gold = (await EthereumObserver.GetUserGoldBalance(user.UserName)).ToString(),
				}
			);
		}

		/// <summary>
		/// Fiat limits
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpGet, Route("limits")]
		[ProducesResponseType(typeof(LimitsView), 200)]
		public async Task<APIResponse> Limits() {

			var user = await GetUserFromDb();
			var limits = await CoreLogic.UserAccount.GetFiatLimits(HttpContext.RequestServices, FiatCurrency.USD, user);

			var curDepositLimit = await CoreLogic.UserAccount.GetCurrentFiatDepositLimit(HttpContext.RequestServices, FiatCurrency.USD, user);
			var curWithdrawLimit = await CoreLogic.UserAccount.GetCurrentFiatWithdrawLimit(HttpContext.RequestServices, FiatCurrency.USD, user);

			return APIResponse.Success(
				new LimitsView() {

					// current user fiat limits
					Current = new LimitsView.UserLimits() {

						Deposit = new LimitsView.UserPeriodLimitItem() {
							Minimal = curDepositLimit.Minimal / 100d,
							Day = curDepositLimit.Day / 100d,
							Month = curDepositLimit.Month / 100d,
						},

						Withdraw = new LimitsView.UserPeriodLimitItem() {
							Minimal = curWithdrawLimit.Minimal / 100d,
							Day = curWithdrawLimit.Day / 100d,
							Month = curWithdrawLimit.Month / 100d,
						},
					},

					// limits by verification level and current user level
					Levels = new LimitsView.VerificationLevels() {

						Current = new LimitsView.VerificationLevelLimits() {

							Deposit = new LimitsView.PeriodLimitItem() {
								Day = limits.Current.Deposit.Day / 100d,
								Month = limits.Current.Deposit.Month / 100d,
							},
							Withdraw = new LimitsView.PeriodLimitItem() {
								Day = limits.Current.Withdraw.Day / 100d,
								Month = limits.Current.Withdraw.Month / 100d,
							}
						},

						L0 = new LimitsView.VerificationLevelLimits() {

							Deposit = new LimitsView.PeriodLimitItem() {
								Day = limits.Level0.Deposit.Day / 100d,
								Month = limits.Level0.Deposit.Month / 100d,
							},
							Withdraw = new LimitsView.PeriodLimitItem() {
								Day = limits.Level0.Withdraw.Day / 100d,
								Month = limits.Level0.Withdraw.Month / 100d,
							}
						},

						L1 = new LimitsView.VerificationLevelLimits() {

							Deposit = new LimitsView.PeriodLimitItem() {
								Day = limits.Level1.Deposit.Day / 100d,
								Month = limits.Level1.Deposit.Month / 100d,
							},
							Withdraw = new LimitsView.PeriodLimitItem() {
								Day = limits.Level1.Withdraw.Day / 100d,
								Month = limits.Level1.Withdraw.Month / 100d,
							}
						}
					},

					// limits per payment method
					PaymentMethod = new LimitsView.PaymentMethods() {

						Card = new LimitsView.PaymentMethodLimits() {
							Deposit = new LimitsView.OnetimeLimitItem() {
								Min = AppConfig.Constants.CardPaymentData.DepositMin / 100d,
								Max = AppConfig.Constants.CardPaymentData.DepositMax / 100d,
							},
							Withdraw = new LimitsView.OnetimeLimitItem() {
								Min = AppConfig.Constants.CardPaymentData.WithdrawMin / 100d,
								Max = AppConfig.Constants.CardPaymentData.WithdrawMax / 100d,
							}
						},

						Swift = new LimitsView.PaymentMethodLimits() {
							Deposit = new LimitsView.OnetimeLimitItem() {
								Min = AppConfig.Constants.SwiftData.DepositMin / 100d,
								Max = AppConfig.Constants.SwiftData.DepositMax / 100d,
							},
							Withdraw = new LimitsView.OnetimeLimitItem() {
								Min = AppConfig.Constants.SwiftData.WithdrawMin / 100d,
								Max = AppConfig.Constants.SwiftData.WithdrawMax / 100d,
							}
						}
					}
				}
			);
		}

		/// <summary>
		/// Profile info
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpGet, Route("profile")]
		[ProducesResponseType(typeof(ProfileView), 200)]
		public async Task<APIResponse> Profile() {

			var user = await GetUserFromDb();

			// user challenges
			var challenges = new List<string>();
			if (!user.UserOptions.InitialTFAQuest) challenges.Add("2fa");
			if (!user.UserOptions.PrimaryAgreementRead) challenges.Add("agreement");

			return APIResponse.Success(
				new ProfileView() {
					Id = user.UserName,
					Name = CoreLogic.UserAccount.IsUserVerifiedL0(user) ? (user.UserVerification.FirstName + " " + user.UserVerification.LastName).Trim() : user.UserName,
					Email = user.Email ?? "",
					TfaEnabled = user.TwoFactorEnabled,
					VerifiedL0 = CoreLogic.UserAccount.IsUserVerifiedL0(user),
					VerifiedL1 = CoreLogic.UserAccount.IsUserVerifiedL1(user),
					Challenges = challenges.ToArray(),
				}
			);
		}

		/// <summary>
		/// User activity
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("activity")]
		[ProducesResponseType(typeof(ActivityView), 200)]
		public async Task<APIResponse> Activity([FromBody] ActivityModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.UserActivity, object>>>() {
				{ "date", _ => _.TimeCreated },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			var query = (
				from a in DbContext.UserActivity
				where a.UserId == user.Id
				select a
			);

			var page = await DalExtensions.PagerAsync(query, model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new ActivityViewItem() {
					Type = i.Type.ToLower(),
					Comment = i.Comment,
					Ip = i.Ip,
					Agent = i.Agent,
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
				}
			;

			return APIResponse.Success(
				new ActivityView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}

		/// <summary>
		/// Fiat history
		/// </summary>
		[AreaAuthorized, AccessRights(AccessRights.Client)]
		[HttpPost, Route("fiat/history")]
		[ProducesResponseType(typeof(FiatHistoryView), 200)]
		public async Task<APIResponse> FiatHistory([FromBody] FiatHistoryModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.FinancialHistory, object>>>() {
				{ "date",   _ => _.TimeCreated },
				{ "amount", _ => _.AmountCents },
				{ "type",   _ => _.Type },
				{ "fee",    _ => _.FeeCents },
			};

			// validate
			if (BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();

			var query = (
				from a in DbContext.FinancialHistory
				where a.UserId == user.Id
				select a
			);

			var page = await DalExtensions.PagerAsync(query, model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new FiatHistoryViewItem() {
					Type = i.Type.ToString().ToLower(),
					Comment = i.Comment,
					Amount = FiatHistoryViewItem.AmountStruct.Create(i.AmountCents, i.Currency),
					Fee = i.FeeCents > 0 ? FiatHistoryViewItem.AmountStruct.Create(i.FeeCents, i.Currency) : null,
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
				}
			;

			return APIResponse.Success(
				new FiatHistoryView() {
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}
	}
}