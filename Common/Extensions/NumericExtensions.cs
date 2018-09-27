using System.Numerics;

namespace Goldmint.Common.Extensions {

	public static class NumericExtensions {

		public static decimal FromEther(this BigInteger v) {
			return (decimal) v / (decimal)BigInteger.Pow(10, TokensPrecision.Ethereum);
		}

		public static decimal FromSumus(this BigInteger v) {
			return (decimal)v / (decimal)BigInteger.Pow(10, TokensPrecision.Sumus);
		}

		public static BigInteger ToEther(this decimal v) {
			return new BigInteger(decimal.Floor(v * (decimal) BigInteger.Pow(10, TokensPrecision.Ethereum)));
		}

		public static BigInteger ToSumus(this decimal v) {
			return new BigInteger(decimal.Floor(v * (decimal) BigInteger.Pow(10, TokensPrecision.Sumus)));
		}
	}
}
