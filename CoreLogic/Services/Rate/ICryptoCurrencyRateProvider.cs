using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface ICryptoCurrencyRateProvider {

		Task<CryptoRate> RequestCryptoRate(TimeSpan timeout);
	}
}
