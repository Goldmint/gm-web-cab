using System;
using System.Collections.Generic;
using System.Text;
using Goldmint.Common;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class SafeRatesFiatAdapter {

		private readonly IAggregatedSafeRatesSource _source;

		public SafeRatesFiatAdapter(IAggregatedSafeRatesSource source) {
			_source = source;
		}

		// ---

		public long? GetRateForBuying(CurrencyRateType currency, FiatCurrency fiatCurrency) {
			var rate = _source.GetRate(currency);

			if (rate.CanBuy) return null;

			switch (fiatCurrency) {
				case FiatCurrency.Usd: return rate.Usd;
			}

			throw new NotImplementedException("Fiat currency is not implemented");
		}

		public long? GetRateForSelling(CurrencyRateType currency, FiatCurrency fiatCurrency) {
			var rate = _source.GetRate(currency);

			if (rate.CanSell) return null;

			switch (fiatCurrency) {
				case FiatCurrency.Usd: return rate.Usd;
			}

			throw new NotImplementedException("Fiat currency is not implemented");
		}
	}
}
