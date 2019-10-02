using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum {

	public interface IEthereumReader {

		/// <summary>
		/// Get ether balance
		/// </summary>
		Task<BigInteger> GetEtherBalance(string address);

		/// <summary>
		/// Get latest block number on the logs provider side
		/// </summary>
		Task<BigInteger> GetLogsLatestBlockNumber();

		/// <summary>
		/// Check chain transaction by it's ID
		/// </summary>
		Task<TransactionInfo> CheckTransaction(string txid, int confirmationsRequired);

		/// <summary>
		/// Get current gas price
		/// </summary>
		Task<BigInteger> GetCurrentGasPrice();

		/// <summary>
		/// Get MNT balance
		/// </summary>
		Task<BigInteger> GetAddressMntBalance(string address);
		
		/// <summary>
		/// Get TokenBuyRequest events
		/// </summary>
		Task<GatheredPoolFreezerEvents> GatherPoolFreezerEvents(BigInteger from, BigInteger to, BigInteger confirmationsRequired);
	}
}
