using Goldmint.CoreLogic.Services.Rate.Models;
using System;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedRatesDispatcher {

		void OnProviderCurrencyRate(CurrencyRate rate);
	}
}
