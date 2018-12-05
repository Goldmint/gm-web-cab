using System;
using System.Collections.Generic;
using System.Numerics;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus {

	public interface ISumusReader {
		
		/// <summary>
		/// Get transaction info or null (not found)
		/// </summary>
		Task<TransactionInfo> GetTransactionInfo(string digest);

		/// <summary>
		/// Get blocks count
		/// </summary>
		Task<ulong> GetBlocksCount();

		/// <summary>
		/// Get block info or null (not found)
		/// </summary>
		Task<BlockInfo> GetBlockInfo(ulong id);

		/// <summary>
		/// Get wallet incoming transactions at specified block ID
		/// </summary>
		Task<List<TransactionInfo>> GetWalletIncomingTransactions(string destinationWallet, ulong blockId);

		/// <summary>
		/// Get wallet state
		/// </summary>
		Task<WalletState> GetWalletState(string addr);
	}
}
