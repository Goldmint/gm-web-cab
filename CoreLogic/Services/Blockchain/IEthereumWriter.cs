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

		/// <summary>
		/// GOLD/ETH buy/sell request processing
		/// </summary>
		Task<string> ProcessBuySellRequest(BigInteger requestIndex, BigInteger ethPerGold);

		/// <summary>
		/// GOLD/ETH buy/sell request cancellation
		/// </summary>
		Task<string> CancelBuySellRequest(BigInteger requestIndex);
	}
}
