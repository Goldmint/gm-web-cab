using Goldmint.CoreLogic.Services.Rate.Models;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IAggregatedSafeRatesSource {

		SafeGoldRate GetGoldRate();
		SafeCryptoRate GetCryptoRate();
	}
}
