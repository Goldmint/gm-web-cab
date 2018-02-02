using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.UserModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.API {

	public partial class UserController : BaseController {

		/// <summary>
		/// User balance on blockchain
		/// </summary>
		[AreaAuthorized]
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
					Usd = await EthereumObserver.GetUserFiatBalance(user.Id, FiatCurrency.USD) / 100d,
					Gold = model.EthAddress == null ? 0d : CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(await EthereumObserver.GetUserGoldBalance(model.EthAddress), false),
					Mntp = model.EthAddress == null ? 0d : CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(await EthereumObserver.GetUserMntpBalance(model.EthAddress), false),
				}
			);
		}

		/// <summary>
		/// Fiat limits
		/// </summary>
		[AreaAuthorized]
		[HttpGet, Route("limits")]
		[ProducesResponseType(typeof(LimitsView), 200)]
		public async Task<APIResponse> Limits() {

			var user = await GetUserFromDb();
			var limits = await CoreLogic.UserAccount.GetFiatLimits(HttpContext.RequestServices, FiatCurrency.USD, user);

			var curDepositLimit = await CoreLogic.UserAccount.GetCurrentFiatDepositLimit(HttpContext.RequestServices, FiatCurrency.USD, user);
			var curWithdrawLimit = await CoreLogic.UserAccount.GetCurrentFiatWithdrawLimit(HttpContext.RequestServices, FiatCurrency.USD, user);

			return APIResponse.Success(
				new LimitsView() {

					// current user
					Current = new LimitsView.UserLimits() {

						Deposit = new LimitsView.UserLimitItem() {
							Current = curDepositLimit / 100d,
							Day = limits.CurrentUser.DayDeposit / 100d,
							Month = limits.CurrentUser.MonthDeposit / 100d,
						},
						Withdraw = new LimitsView.UserLimitItem() {
							Current = curWithdrawLimit / 100d,
							Day = limits.CurrentUser.DayWithdraw / 100d,
							Month = limits.CurrentUser.MonthWithdraw / 100d,
						},
					},

					// levels
					Levels = new LimitsView.VerificationLevels() {

						L0 = new LimitsView.VerificationLevelLimits() {

							Deposit = new LimitsView.LimitItem() {
								Day = limits.Level0.DayDeposit / 100d,
								Month = limits.Level0.MonthDeposit / 100d,
							},
							Withdraw = new LimitsView.LimitItem() {
								Day = limits.Level0.DayWithdraw / 100d,
								Month = limits.Level0.MonthWithdraw / 100d,
							}
						},

						L1 = new LimitsView.VerificationLevelLimits() {

							Deposit = new LimitsView.LimitItem() {
								Day = limits.Level1.DayDeposit / 100d,
								Month = limits.Level1.MonthDeposit / 100d,
							},
							Withdraw = new LimitsView.LimitItem() {
								Day = limits.Level1.DayWithdraw / 100d,
								Month = limits.Level1.MonthWithdraw / 100d,
							}
						}
					},
				}
			);
		}

		/// <summary>
		/// Profile info
		/// </summary>
		[AreaAuthorized]
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
		[AreaAuthorized]
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

			var page = await query.PagerAsync(
				model.Offset, model.Limit,
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
		[AreaAuthorized]
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

			var page = await query.PagerAsync(
				model.Offset, model.Limit,
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