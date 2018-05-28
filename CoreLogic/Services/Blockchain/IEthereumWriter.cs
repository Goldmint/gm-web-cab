using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public interface IEthereumWriter {

		/// <summary>
		/// Transfer GOLD from hot wallet to an address
		/// </summary>
		Task<string> TransferGoldFromHotWallet(string userId, string userAddress, BigInteger amount);

		/// <summary>
		/// Buy/sell-for-ETH request processing
		/// </summary>
		Task<string> ProcessRequestEth(BigInteger requestIndex, BigInteger ethPerGold);

		/// <summary>
		/// Add and process buy-for-fiat request at the same time
		/// </summary>
		Task<string> ProcessBuyRequestFiat(string userId, BigInteger reference, string userAddress, long amountCents, long centsPerGold);

		/// <summary>
		/// Process sell-for-fiat request
		/// </summary>
		Task<string> ProcessSellRequestFiat(BigInteger requestIndex, long centsPerGold);

		/// <summary>
		/// Buy/sell request cancellation
		/// </summary>
		Task<string> CancelRequest(BigInteger requestIndex);
	}
}
