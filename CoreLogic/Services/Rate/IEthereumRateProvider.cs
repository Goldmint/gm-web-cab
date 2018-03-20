using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IEthereumRateProvider {

		/// <summary>
		/// Price in cents per token
		/// </summary>
		Task<long> GetEthereumRate(FiatCurrency currency);
	}
}
