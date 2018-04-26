using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Models;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumReader {

		/// <summary>
		/// Check chain transaction by it's ID
		/// </summary>
		/// <returns>Transaction status by ID</returns>
		Task<TransactionInfo> CheckTransaction(string txid, int confirmationsRequired);

		/// <summary>
		/// Get current gas price
		/// </summary>
		/// <returns>Transaction status by ID</returns>
		Task<BigInteger> GetCurrentGasPrice();

		/// <summary>
		/// Get MNTP balance
		/// </summary>
		/// <returns>MNTP amount at specified address</returns>
		Task<BigInteger> GetAddressMntpBalance(string address);

		/// <summary>
		/// Get GOLD balance
		/// </summary>
		/// <returns>GOLD amount at specified address</returns>
		Task<BigInteger> GetAddressGoldBalance(string address);

		/// <summary>
		/// Get hot wallet GOLD balance
		/// </summary>
		/// <returns>User GOLD amount</returns>
		Task<BigInteger> GetHotWalletGoldBalance(string userId);

		// ---
		
		/// <summary>
		/// Buy/sell ETH requests current count
		/// </summary>
		/// <returns>Requests count</returns>
		Task<BigInteger> GetBuySellRequestsCount();

		/// <summary>
		/// GOLD buy/sell request data by it's index
		/// </summary>
		/// <returns>Request data and status</returns>
		Task<GoldEthExchangeRequest> GetBuySellRequestByIndex(BigInteger requestIndex);

		/// <summary>
		/// Get `TokenBuyRequest` events
		/// </summary>
		Task<GatheredGoldBoughtWithEthEvent> GatherTokenBuyRequestEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired);

		/// <summary>
		/// Get `TokenSellRequest` events
		/// </summary>
		Task<GatheredGoldSoldForEthEvent> GatherTokenSellRequestEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired);
	}
}
