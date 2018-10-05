using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Models {

	public sealed class WalletState {

		public BalanceData Balance { get; internal set; }
		public bool Exist { get; internal set; }
		public ulong LastNonce { get; internal set; }
		public string[] Tags { get; internal set; }

		public sealed class BalanceData {
			public BigInteger Gold { get; internal set; }
			public BigInteger Mnt { get; internal set; }
		}
	}
}
