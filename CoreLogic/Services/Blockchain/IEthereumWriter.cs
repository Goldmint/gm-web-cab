using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumWriter {

		/// <summary>
		/// Change balance on deposit/withdraw ops
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> ChangeUserFiatBalance(string userId, FiatCurrency currency, long amountCents);

		/// <summary>
		/// Process exchange request by it's index
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> ProcessExchangeRequest(BigInteger requestIndex, FiatCurrency currency, long amountCents, long centsPerGoldToken);
		
		/// <summary>
		/// Cancel exchange request by it's index
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> CancelExchangeRequest(BigInteger requestIndex);
	}
}
