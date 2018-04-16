using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Rate.Models;
using System;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedSafeRatesPublisher {

		Task PublishRates(DateTime timestamp, SafeGoldRate goldRate, SafeCryptoRate cryptoRate);
	}
}
