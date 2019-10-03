using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus {

	public interface IConnPool {

		Task<NATS.Client.IConnection> GetConnection();
	}
}
