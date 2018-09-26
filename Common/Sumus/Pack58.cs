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

		public static bool Unpack(string addr, out byte[] data) {
			if (!BitConverter.IsLittleEndian) throw new Exception("Big-endian is not supported");

			data = null;
			try {
				var bytes = Multiformats.Base.Multibase.Base58.Decode(addr);
				if (bytes != null && bytes.Length > 4) {
					var payloadBytes = bytes.Take(bytes.Length - 4).ToArray();
					var crcBytes = bytes.Skip(bytes.Length - 4).Take(4).ToArray();
					var payloadCrc = Crc32.Crc32Algorithm.Compute(payloadBytes);
					var crc = BitConverter.ToUInt32(crcBytes, 0);
					data = payloadBytes;
					return payloadCrc == crc;
				}
			}
			catch {}
			return false;
		}

		public static string PackHash(byte[] addr, ulong nonce) {
			if (addr == null || addr.Length != 32) {
				throw new ArgumentException("Invalid address");
			}

			var data = addr.Concat(BitConverter.GetBytes(nonce)).ToArray();
			return Pack(data);
		}

		public static bool UnpackHash(string hash, out byte[] addr, out ulong nonce) {
			if (string.IsNullOrWhiteSpace(hash)) {
				throw new ArgumentException("Invalid hash");
			}

			addr = null;
			nonce = 0;

			if (!Unpack(hash, out var data) || data == null || data.Length != 32+8) {
				return false;
			}

			addr = data.Take(32).ToArray();
			nonce = BitConverter.ToUInt64(data, 32);
			return true;
		}
	}
}
