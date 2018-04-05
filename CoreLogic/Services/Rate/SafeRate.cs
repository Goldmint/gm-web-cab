using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Rate {

	public sealed class SafeRate {

		public readonly long Rate;
		public readonly bool IsSafeToBuy;
		public readonly bool IsSafeToSell;

		public SafeRate(long rate, bool safeToBuy, bool safeToSell) {
			Rate = rate;
			IsSafeToBuy = safeToBuy;
			IsSafeToSell = safeToSell;
		}
	}
}
