using System;
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

		public Transaction() {
		}

		// ---

		private static Transaction Construct(Signer signer, ulong nonce, Func<Serializer, string> write) {

			var txname = "";
			var txdata = "";
			var txhash = "";

			using (var s = new Serializer()) {
				s.Write(nonce);
				txname = write(s);

				byte[] digest;
				using (var hasher = SHA512.Create()) { // TODO: implement sha-3 256 bit
					digest = hasher.ComputeHash(s.Data());
				}

				var signature = signer.Sign(digest);

				s.Write((byte)1);
				s.Write(signature);

				txdata = s.Hex();
				txhash = Pack58.PackHash(signer.PublicKeyBytes, nonce);
			}

			return new Transaction() {
				Data = txdata,
				Hash = txhash,
				Name = txname,
				Nonce = nonce,
			};
		}

		// ---

		public static Transaction TransferAsset(Signer signer, ulong nonce, byte[] addr, Amount amount) {
			if (signer == null || addr == null || addr.Length != 32 || amount.Token == null) {
				throw new ArgumentException("Invalid signer, address or amount (token) specified");
			}
			return Construct(signer, nonce, (Serializer s) => {
				s.Write((ushort)amount.Token.Value);
				s.Write(signer.PublicKeyBytes);
				s.Write(addr);
				s.Write(amount);
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
