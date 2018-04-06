using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface ISafeCryptoassetRateProvider {

		/// <summary>
		/// Price in cents per asset
		/// </summary>
		Task<SafeRate> GetRate(CryptoCurrency asset, FiatCurrency currency);
	}
}
