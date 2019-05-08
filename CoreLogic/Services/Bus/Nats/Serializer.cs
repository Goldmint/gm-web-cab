using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Goldmint.CoreLogic.Services.Bus.Nats {

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
