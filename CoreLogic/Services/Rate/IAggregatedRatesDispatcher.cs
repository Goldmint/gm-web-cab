using Goldmint.CoreLogic.Services.Rate.Models;
using System;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedRatesDispatcher {

		void OnGoldRate(GoldRate rate, TimeSpan expectedPeriod);
		void OnCryptoRate(CryptoRate rate, TimeSpan expectedPeriod);
	}
}
