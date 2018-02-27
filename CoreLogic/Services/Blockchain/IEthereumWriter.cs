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
		
		/// <summary>
		/// Process hot wallet exchange request
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> ProcessHotWalletExchangeRequest(string userId, bool isBuying, FiatCurrency currency, long amountCents, long centsPerGoldToken);
		
		/// <summary>
		/// Transfer GOLD from hot wallet to address
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> TransferGoldFromHotWallet(string userId, string toAddress, BigInteger amount);
	}
}
