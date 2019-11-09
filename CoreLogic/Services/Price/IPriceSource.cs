using Goldmint.Common;

namespace Goldmint.CoreLogic.Services.Price {

	public interface IPriceSource {

		// GetPriceInFiat returns currency price in fiat currency or null in case the source is not ready
		long? GetPriceInFiat(CurrencyPrice currency, FiatCurrency fiatCurrency);
	}
}
