using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.CommonsModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Controllers.v1 {

	[Route("api/v1/commons")]
	public class CommonsController : BaseController {

		/// <summary>
		/// Price per gold ounce
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("goldRate")]
		[ProducesResponseType(typeof(GoldRateView), 200)]
		public async Task<APIResponse> GoldRate() {
			return APIResponse.Success(
				new GoldRateView() {
					Rate = await GoldRateCached.GetGoldRate(Common.FiatCurrency.USD) / 100d,
				}
			);
		}
		
		/// <summary>
		/// Price per ETH
		/// </summary>
		[AnonymousAccess]
		[HttpGet, Route("ethRate")]
		[ProducesResponseType(typeof(EthRateView), 200)]
		public async Task<APIResponse> EthRate() {

			var usd = await CryptoassetRateProvider.GetRate(CryptoExchangeAsset.ETH, FiatCurrency.USD);
			usd = usd - (long)Math.Round(usd * AppConfig.Constants.CryptoExchange.DepositFiatConversionBuffer);

			return APIResponse.Success(
				new EthRateView() {
					Usd = usd / 100d,
				}
			);
		}

		/// <summary>
		/// Transparency
		/// </summary>
		[AnonymousAccess]
		[HttpPost, Route("transparency")]
		[ProducesResponseType(typeof(TransparencyView), 200)]
		public async Task<APIResponse> Transparency([FromBody] TransparencyModel model) {

			var sortExpression = new Dictionary<string, System.Linq.Expressions.Expression<Func<DAL.Models.Transparency, object>>>() {
				{ "date",   _ => _.TimeCreated },
				{ "amount", _ => _.Amount },
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
					Amount = i.Amount / 100d,
					Link = string.Format("https://ipfs.io/ipfs/{0}", i.Hash),
					Date = ((DateTimeOffset)i.TimeCreated).ToUnixTimeSeconds(),
				}
			;

			var stat = await DbContext.TransparencyStat.AsNoTracking().LastOrDefaultAsync();
			var statAssets = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.AssetsArray ?? "[]");
			var statBonds = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.BondsArray ?? "[]");
			var statFiat = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.FiatArray ?? "[]");
			var statGold = Common.Json.Parse<TransparencyViewStatItem[]>(stat?.GoldArray ?? "[]");
			var statDataTime = stat?.DataTimestamp ?? DateTime.UtcNow;
			var statAuditTime = stat?.AuditTimestamp ?? DateTime.UtcNow;

			return APIResponse.Success(
				new TransparencyView() {
					Stat = new TransparencyViewStat() {
						Assets = statAssets,
						Bonds = statBonds,
						Fiat = statFiat,
						Gold = statGold,
						DataTimestamp = ((DateTimeOffset)statDataTime).ToUnixTimeSeconds(),
						AuditTimestamp = ((DateTimeOffset)statAuditTime).ToUnixTimeSeconds(),
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
		[ProducesResponseType(typeof(string[]), 200)]
		public async Task<APIResponse> BannedCountries() {
			var list = await (
				from a in DbContext.BannedCountry
				select a.Code.ToUpper()
			).AsNoTracking().ToArrayAsync();
			return APIResponse.Success(list);
		}
	}
}