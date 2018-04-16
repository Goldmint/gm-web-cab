using System;

namespace Goldmint.CoreLogic.Services.Rate.Models {

	public class CryptoRate {

		public long EthUsd { get; internal set; }
		
		public override string ToString() {
			return $"ethUsd={EthUsd};";
		}
	}

	public class SafeCryptoRate : CryptoRate {

		public bool IsSafeForBuy { get; internal set; }
		public bool IsSafeForSell { get; internal set; }

		public override string ToString() {
			return base.ToString() + $"b={ IsSafeForBuy };s={ IsSafeForSell }";
		}
	}
}
