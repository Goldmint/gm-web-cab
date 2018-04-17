using Goldmint.CoreLogic.Services.Rate.Models;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedSafeRatesPublisher {

		Task PublishRates(SafeCurrencyRate[] currencies);
	}
}
