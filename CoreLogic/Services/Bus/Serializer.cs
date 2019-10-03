using System.IO;

namespace Goldmint.CoreLogic.Services.Bus {

	public static class Serializer {

		public static byte[] Serialize<T>(T msg) {
			using (var s = new MemoryStream()) {
				ProtoBuf.Serializer.Serialize<T>(s, msg);
				return s.ToArray();
			}
		}
		
		public static T Deserialize<T>(byte[] bytes) {
			using (var s = new MemoryStream(bytes)) {
				return ProtoBuf.Serializer.Deserialize<T>(s);
			}
		}
	}
}
