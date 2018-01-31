using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IGoldRateProvider {

		/// <summary>
		/// Price in cents per gold ounce
		/// </summary>
		Task<long> GetGoldRate(FiatCurrency currency);
	}
}
