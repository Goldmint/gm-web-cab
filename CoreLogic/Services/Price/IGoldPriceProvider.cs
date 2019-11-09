using Goldmint.CoreLogic.Services.Price.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Price {

	public interface IGoldPriceProvider {

		Task<CurrencyPrice> RequestGoldPrice(TimeSpan timeout);
	}
}
