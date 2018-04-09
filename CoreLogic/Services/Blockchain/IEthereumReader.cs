using System.Collections.Generic;
using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Blockchain.Models;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumReader {

		/// <summary>
		/// Check chain transaction by it's ID
		/// </summary>
		/// <returns>Transaction status by ID</returns>
		Task<EthTransactionStatus> CheckTransaction(string transactionId, int confirmations);

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

		#region GOLD / ETH

		/// <summary>
		/// Get event in a range of blocks
		/// </summary>
		/// <returns>Events</returns>
		Task<GatheredGoldBoughtWithEthEvent> GatherGoldBoughtWithEthEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired);

		/// <summary>
		/// Get event in a range of blocks
		/// </summary>
		/// <returns>Events</returns>
		Task<GatheredGoldSoldForEthEvent> GatherGoldSoldForEthEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired);

		#endregion

		#region GOLD / Fiat

		/// <summary>
		/// Get user's fiat balance in cents
		/// </summary>
		/// <returns>User fiat amount</returns>
		Task<long> GetUserFiatBalance(string userId, FiatCurrency currency);

		/// <summary>
		/// GOLD exchange total requests count
		/// </summary>
		/// <returns>Requests count</returns>
		Task<BigInteger> GetGoldFiatExchangeRequestsCount();

		/// <summary>
		/// GOLD buy/sell request data by it's index
		/// </summary>
		/// <returns>Request data and status</returns>
		Task<GoldFiatExchangeRequest> GetGoldFiatExchangeRequestByIndex(BigInteger requestIndex);

		#endregion
	}
}
