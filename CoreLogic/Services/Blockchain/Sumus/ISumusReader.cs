using System.Collections.Generic;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus {

	public interface ISumusReader {
		
		/// <summary>
		/// Get transaction info or null (not found)
		/// </summary>
		Task<TransactionInfo> GetTransactionInfo(string hash);

		/// <summary>
		/// Get last processed block number
		/// </summary>
		Task<ulong> GetLastBlockNumber();

		/// <summary>
		/// Get incoming transactions in block span, assume that beginBlock >= endBlock
		/// </summary>
		Task<List<TransactionInfo>> GetBlocksSpanTransaction(string destinationWallet, ulong beginBlock, ulong endBlock);

	}
}
