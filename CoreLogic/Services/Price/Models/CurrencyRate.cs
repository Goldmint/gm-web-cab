using Goldmint.Common;
using System;

namespace Goldmint.CoreLogic.Services.Price.Models {

	public class CurrencyPrice {

		public readonly Common.CurrencyPrice Currency;
		public readonly DateTime Stamp;
		public readonly long Usd;
		public readonly long Eur;

		public CurrencyPrice(Common.CurrencyPrice cur, DateTime stamp, long usd, long eur) {
			Currency = cur;
			Stamp = stamp;
			Usd = usd;
			Eur = eur;
		}
	}
}
