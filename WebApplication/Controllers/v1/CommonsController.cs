using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.CommonsModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/commons")]
	public class CommonsController : BaseController {

		/// <summary>
		/// Transparency
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("transparency")]
		[ProducesResponseType(typeof(TransparencyView), 200)]
		public async Task<APIResponse> Transparency([FromBody] TransparencyModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.Transparency, object>>>() {
				{ "date",   _ => _.TimeCreated }
			};

			// validate
			if (Models.API.BasePagerModel.IsInvalid(model, sortExpression.Keys, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var query = (
				from a in DbContext.Transparency
				select a
			);

			var page = await DalExtensions.PagerAsync(query, model.Offset, model.Limit,
				sortExpression.GetValueOrDefault(model.Sort), model.Ascending
			);

			var list =
				from i in page.Selected
				select new TransparencyViewItem() {
					Comment = i.Comment,
					Amount = i.Amount,
					Link = string.Format("https://ipfs.io/ipfs/{0}", i.Hash),
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
				}
			;

			var stat = await DbContext.TransparencyStat.AsNoTracking().LastOrDefaultAsync();
			var statAssets = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.AssetsArray ?? "[]");
			var statBonds = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.BondsArray ?? "[]");
			var statFiat = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.FiatArray ?? "[]");
			var statGold = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.GoldArray ?? "[]");
			var statTotalOz = stat?.TotalOz ?? "";
			var statTotalUsd = stat?.TotalUsd ?? "";
			var statDataTime = stat?.DataTimestamp != null? ((DateTimeOffset)stat.DataTimestamp).ToUnixTimeSeconds(): (long?)null;
			var statAuditTime = stat?.AuditTimestamp != null? ((DateTimeOffset)stat.AuditTimestamp).ToUnixTimeSeconds(): (long?)null;

			return APIResponse.Success(
				new TransparencyView() {
					Stat = new TransparencyViewStat() {
						Assets = statAssets,
						Bonds = statBonds,
						Fiat = statFiat,
						Gold = statGold,
						TotalOz = statTotalOz,
						TotalUsd = statTotalUsd,
						DataTimestamp = statDataTime,
						AuditTimestamp = statAuditTime,
					},
					Items = list.ToArray(),
					Limit = model.Limit,
					Offset = model.Offset,
					Total = page.TotalCount,
				}
			);
		}

		/// <summary>
		/// Countries blacklist
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("bannedCountries")]
		[ProducesResponseType(typeof(BannedCountriesView), 200)]
		public async Task<APIResponse> BannedCountries() {
			var codes = await (
				from a in DbContext.BannedCountry
				select a.Code.ToUpper()
			).AsNoTracking().ToArrayAsync();

			return APIResponse.Success(new BannedCountriesView() {
				Codes = codes,
				CallerBanned = codes.Contains(GetUserCountry()),
			});
		}

		/// <summary>
		/// System status
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpGet, Route("status")]
		[ProducesResponseType(typeof(StatusView), 200)]
		public async Task<APIResponse> Status() {

			var user = await GetUserFromDb();
			var rcfg = RuntimeConfigHolder.Clone();

			var ethDepositLimits = v1.User.BuyGoldController.DepositLimits(rcfg, TradableCurrency.Eth);
			var ethWithdrawLimits = v1.User.SellGoldController.WithdrawalLimits(rcfg, TradableCurrency.Eth);
			var ccDepositLimits = await v1.User.BuyGoldController.DepositLimits(rcfg, DbContext, user.Id, FiatCurrency.Usd);
			var ccWithdrawLimits = await v1.User.SellGoldController.WithdrawalLimits(rcfg, DbContext, user.Id, FiatCurrency.Usd);

			var ret = new StatusView() {
				Trading = new StatusViewTrading() {
					EthAllowed = rcfg.Gold.AllowTradingOverall && rcfg.Gold.AllowTradingEth,
					CreditCardBuyingAllowed = rcfg.Gold.AllowTradingOverall && rcfg.Gold.AllowBuyingCreditCard,
					CreditCardSellingAllowed = rcfg.Gold.AllowTradingOverall && rcfg.Gold.AllowSellingCreditCard,
				},
				Limits = new StatusViewLimits() {
					Eth = new StatusViewLimits.Method() {
						Deposit = new StatusViewLimits.MinMax() {
							Min = ethDepositLimits.Min.ToString(),
							Max = ethDepositLimits.Max.ToString(),
							AccountMax = "0",
							AccountUsed = "0",
						},
						Withdraw = new StatusViewLimits.MinMax() {
							Min = ethWithdrawLimits.Min.ToString(),
							Max = ethWithdrawLimits.Max.ToString(),
							AccountMax = "0",
							AccountUsed = "0",
						}
					},
					CreditCardUsd = new StatusViewLimits.Method() {
						Deposit = new StatusViewLimits.MinMax() {
							Min = (long)ccDepositLimits.Min / 100d,
							Max = (long)ccDepositLimits.Max / 100d,
							AccountMax = (long)ccDepositLimits.AccountMax / 100d,
							AccountUsed = (long)ccDepositLimits.AccountUsed / 100d,
						},
						Withdraw = new StatusViewLimits.MinMax() {
							Min = (long)ccWithdrawLimits.Min / 100d,
							Max = (long)ccWithdrawLimits.Max / 100d,
							AccountMax = (long)ccWithdrawLimits.AccountMax / 100d,
							AccountUsed = (long)ccWithdrawLimits.AccountUsed / 100d,
						}
					},
				},
			};

			return APIResponse.Success(ret);
		}
	}
}