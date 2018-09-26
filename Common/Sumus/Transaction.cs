using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.Common.Sumus {

	public class Transaction {

		public const string TxTransferAsset = "TransferAssetsTransaction";
		public const string TxUserData = "UserDataTransaction";

		// ---

		public readonly string Name;
		public readonly string Data;
		public readonly string Hash;

		public Transaction(string name, string data, string hash) {
			Name = name;
			Data = data;
			Hash = hash;
		}

		// ---

		public static Transaction TransferAsset(Signer signer, ulong nonce, byte[] addr, Amount amount) {
			if (signer == null || addr == null || addr.Length != 32) {
				throw new ArgumentException("Invalid signer or address specified");
			}

			var txdata = "";
			var txhash = "";

			return new Transaction(
				name: TxTransferAsset,
				data: txdata,
				hash: txhash
			);
		}

		public static Transaction UserData(Signer signer, ulong nonce, byte[] data) {
			if (signer == null || data == null || data.Length == 0) {
				throw new ArgumentException("Invalid signer or data specified");
			}

			var txdata = "";
			var txhash = "";

			return new Transaction(
				name: TxUserData,
				data: txdata,
				hash: txhash
			);
		}
	}
}
