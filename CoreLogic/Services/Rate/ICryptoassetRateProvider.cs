using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface ICryptoassetRateProvider {

		/// <summary>
		/// Price in cents per token
		/// </summary>
		Task<long> GetRate(CryptoExchangeAsset asset, FiatCurrency currency);
	}
}
