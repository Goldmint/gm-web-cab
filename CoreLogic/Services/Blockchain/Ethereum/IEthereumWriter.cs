using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum {

	public interface IEthereumWriter {
		
		Task<string> GetEthSender();
		Task<string> SendEth(string address, BigInteger amount);
	}
}
