using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface ISafeGoldRateProvider {

		/// <summary>
		/// Price in cents per gold ounce
		/// </summary>
		Task<SafeRate> GetRate(FiatCurrency currency);
	}
}
