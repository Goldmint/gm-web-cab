namespace Goldmint.CoreLogic.Services.RPC {

	public interface IRPCServer {

		void Start(string address);
		void Stop();
	}

}
