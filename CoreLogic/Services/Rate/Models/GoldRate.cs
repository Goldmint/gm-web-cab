namespace Goldmint.CoreLogic.Services.Rate.Models {

	public class GoldRate {

		public long Usd { get; internal set; }

		public override string ToString() {
			return $"usd={Usd};";
		}
	}

	public class SafeGoldRate : GoldRate {

		public bool IsSafeForBuy { get; internal set; }
		public bool IsSafeForSell { get; internal set; }

		public override string ToString() {
			return base.ToString() + $"b={ IsSafeForBuy };s={ IsSafeForSell }";
		}
	}
}
