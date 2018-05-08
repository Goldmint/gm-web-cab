using NetMQ;

namespace Goldmint.CoreLogic.Services.Bus {

	public static class PubSubCommon {

		public static bool ReceiveMessage(NetMQSocket socket, out string topic, out string stamp, out byte[] message) {
			topic = null;
			stamp = null;
			message = null;

			var hm = false;
			topic = socket.ReceiveFrameString();
			stamp = socket.ReceiveFrameString();
			message = socket.ReceiveFrameBytes(out hm);

			return hm;
		}

	}
}
