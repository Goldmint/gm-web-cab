using Goldmint.Common;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus {

	public interface ISumusWriter {

		/// <summary>
		/// Send token amount to the address
		/// </summary>
		Task<SentTransaction> TransferToken(byte[] privateKey, ulong nonce, byte[] addr, SumusToken asset, decimal amount);
	}
}
