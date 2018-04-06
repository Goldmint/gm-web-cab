using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface ICryptoassetRateProvider {

		/// <summary>
		/// Price in cents per asset
		/// </summary>
		Task<long> GetRate(CryptoCurrency asset, FiatCurrency currency);
	}
}
