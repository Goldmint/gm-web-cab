using Goldmint.Common;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Blockchain.Sumus.Models;
using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus {

	public interface ISumusWriter {

		/// <summary>
		/// Post transaction
		/// </summary>
		Task<SentTransaction> TransferToken(Common.Sumus.Signer signer, ulong nonce, byte[] addr, SumusToken asset, BigInteger amount);
	}
}
