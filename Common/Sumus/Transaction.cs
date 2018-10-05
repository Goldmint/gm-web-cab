using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Goldmint.Common.Sumus {

	public class Transaction {

		public const string TxTransferAsset = "TransferAssetsTransaction";
		public const string TxUserData = "UserDataTransaction";

		// ---

		public ulong Nonce;
		public string Name;
		public string Data;
		public string Hash;
		public string Digest;

		public Transaction() {
		}

		// ---

		private static Transaction Construct(Signer signer, ulong nonce, Func<Serializer, string> write) {

			var txname = "";
			var txdata = "";
			var txhash = "";
			var txdigest = "";

			using (var s = new Serializer()) {
				s.Write(nonce);
				txname = write(s);

				var payload = s.Data();
				var hasher = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256);
				hasher.BlockUpdate(payload, 0, payload.Length);

				var digest = new byte[32];
				hasher.DoFinal(digest, 0);

				var signature = signer.Sign(digest);

				s.Write((byte)1);
				s.Write(signature);

				txdata = s.Hex();
				txhash = Pack58.PackHash(signer.PublicKeyBytes, nonce);
				txdigest = Pack58.Pack(digest);
			}

			return new Transaction() {
				Data = txdata,
				Hash = txhash,
				Name = txname,
				Nonce = nonce,
				Digest = txdigest,
			};
		}

		// ---

		public static Transaction TransferAsset(Signer signer, ulong nonce, byte[] addr, SumusToken token, BigInteger amount) {
			if (signer == null || addr == null || addr.Length != 32) {
				throw new ArgumentException("Invalid signer, address or amount (token) specified");
			}
			return Construct(signer, nonce, (Serializer s) => {
				s.Write(token);
				s.Write(signer.PublicKeyBytes);
				s.Write(addr);
				s.WriteAmount(amount);
				return TxTransferAsset;
			});
		}

		public static Transaction UserData(Signer signer, ulong nonce, byte[] data) {
			if (signer == null || data == null || data.Length == 0) {
				throw new ArgumentException("Invalid signer or data specified");
			}
			return Construct(signer, nonce, (Serializer s) => {
				s.Write(signer.PublicKeyBytes);
				s.Write((uint)data.Length);
				s.Write(data);
				return TxUserData;
			});
		}
	}
}
