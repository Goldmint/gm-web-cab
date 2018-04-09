using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumWriter {

		/// <summary>
		/// Transfer GOLD from hot wallet to an address
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> TransferGoldFromHotWallet(string userId, string toAddress, BigInteger amount);

		#region GOLD / Fiat

		/// <summary>
		/// Change user fiat balance
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> ChangeFiatBalance(string userId, FiatCurrency currency, long amountCents);

		/// <summary>
		/// Process GOLD buying request by it's index
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> PerformGoldFiatExchangeRequest(BigInteger requestIndex, FiatCurrency currency, long amountCents, long centsPerGoldToken);

		/// <summary>
		/// Cancel GOLD buying request by it's index
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> CancelGoldFiatExchangeRequest(BigInteger requestIndex);

		/// <summary>
		/// Process hot wallet exchange request
		/// </summary>
		/// <returns>Transaction ID</returns>
		Task<string> ExchangeGoldFiatOnHotWallet(string userId, bool isBuying, FiatCurrency currency, long amountCents, long centsPerGoldToken);

		#endregion
	}
}
