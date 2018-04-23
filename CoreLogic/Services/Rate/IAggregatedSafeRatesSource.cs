using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedSafeRatesSource {

		SafeCurrencyRate GetRate(CurrencyRateType cur);
	}
}
