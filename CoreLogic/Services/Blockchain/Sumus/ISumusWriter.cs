using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus {

	public interface ISumusWriter {

		/// <summary>
		/// Send token amount to the address
		/// </summary>
		Task<string> TransferToken(string toAddress, MigrationRequestAsset asset, decimal amount);
	}
}
