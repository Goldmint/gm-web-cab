using Google.Protobuf;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus {

	public interface IBus {

		Task<NATS.Client.IConnection> AllocateConnection();
		Task<NATS.Client.IConnection> RequireConnection(int timeoutMs = 30000);
		Task ReleaseConnection(NATS.Client.IConnection c);

		Task<Trep> Request<Treq, Trep>(string subject, Treq request, MessageParser<Trep> replyParser, int timeoutMs = 30000) where Treq : IMessage where Trep : IMessage<Trep>;
	}
}
