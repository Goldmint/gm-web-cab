using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goldmint.Common.Sumus {

	public static class Pack58 {

		public static string Pack(byte[] data) {
			if (!BitConverter.IsLittleEndian) throw new Exception("Big-endian is not supported");
			if (data == null || data.Length == 0) {
				throw new ArgumentException("Data is null or empty");
			}

			var crc = BitConverter.GetBytes(Crc32.Crc32Algorithm.Compute(data));
			return Multiformats.Base.Multibase.Base58.Encode(data.Concat(crc).ToArray());
		}

		public static bool Unpack(string str, out byte[] data) {
			if (!BitConverter.IsLittleEndian) throw new Exception("Big-endian is not supported");

			data = null;
			try {
				var bytes = Multiformats.Base.Multibase.Base58.Decode(str);
				if (bytes != null && bytes.Length > 4) {
					var payloadBytes = bytes.Take(bytes.Length - 4).ToArray();
					var crcBytes = bytes.Skip(bytes.Length - 4).Take(4).ToArray();
					var payloadCrc = Crc32.Crc32Algorithm.Compute(payloadBytes);
					var crc = BitConverter.ToUInt32(crcBytes, 0);
					data = payloadBytes;
					return payloadCrc == crc;
				}
			}
			catch { }
			return false;
		}

		public static bool IsAddress(string addr) {
			if (!BitConverter.IsLittleEndian) throw new Exception("Big-endian is not supported");

			try {
				var bytes = Multiformats.Base.Multibase.Base58.Decode(addr);
				if (bytes != null && bytes.Length == 36) {
					var payloadBytes = bytes.Take(bytes.Length - 4).ToArray();
					var crcBytes = bytes.Skip(bytes.Length - 4).Take(4).ToArray();
					var payloadCrc = Crc32.Crc32Algorithm.Compute(payloadBytes);
					var crc = BitConverter.ToUInt32(crcBytes, 0);
					return payloadCrc == crc;
				}
			}
			catch { }
			return false;
		}
	}
}
