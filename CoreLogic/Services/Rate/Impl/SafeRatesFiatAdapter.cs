using Goldmint.Common;
using System;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public sealed class SafeRatesFiatAdapter {

		private readonly IAggregatedSafeRatesSource _source;

		public SafeRatesFiatAdapter(IAggregatedSafeRatesSource source) {
			_source = source;
		}

		// ---

		public long? GetRateForBuying(CurrencyRateType currency, FiatCurrency fiatCurrency) {
			var rate = _source.GetRate(currency);
			if (!rate.CanBuy) return null;
			return ExtractFiatRate(rate, fiatCurrency);
		}

		public long? GetRateForSelling(CurrencyRateType currency, FiatCurrency fiatCurrency) {
			var rate = _source.GetRate(currency);
			if (!rate.CanSell) return null;
			return ExtractFiatRate(rate, fiatCurrency);
		}

		public static long? ExtractFiatRate(Models.SafeCurrencyRate rate, FiatCurrency fiatCurrency) {
			switch (fiatCurrency) {
				case FiatCurrency.Usd: return rate.Usd;
			}
			throw new NotImplementedException("Fiat currency is not implemented");
		}
	}
}
