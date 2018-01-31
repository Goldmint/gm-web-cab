using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumReader {

		/// <summary>
		/// Check chain transaction by it's ID
		/// </summary>
		/// <returns>Transaction status by ID</returns>
		Task<BlockchainTransactionStatus> CheckTransaction(string transactionId);

		/// <summary>
		/// Get user's Mint balance
		/// </summary>
		/// <returns>MNTP amount at specified address</returns>
		Task<BigInteger> GetUserMntpBalance(string address);

		/// <summary>
		/// Get user's Gold balance
		/// </summary>
		/// <returns>GOLD amount at specified address</returns>
		Task<BigInteger> GetUserGoldBalance(string address);

		/// <summary>
		/// Get user's fiat balance in cents
		/// </summary>
		/// <returns>User fiat amount</returns>
		Task<long> GetUserFiatBalance(long userId, FiatCurrency currency);

		/// <summary>
		/// Gold exchange total requests count
		/// </summary>
		/// <returns>Requests count</returns>
		Task<BigInteger> GetExchangeRequestsCount();

		/// <summary>
		/// Exchange request data by it's index
		/// </summary>
		/// <returns>Request data and status</returns>
		Task<ExchangeRequestData> GetExchangeRequestByIndex(BigInteger requestIndex);
	}
}
