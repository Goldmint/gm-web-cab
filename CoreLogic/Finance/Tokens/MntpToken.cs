using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Goldmint.CoreLogic.Finance.Tokens {

	public static class MntpToken {

		public static readonly int TokenPercision = 18;
		public static readonly long TokenPercisionMultiplier = (long)Math.Pow(10d, (double)TokenPercision);

		public static readonly int VisualTokenPercision = 6;
		public static readonly long VisualTokenPercisionMultiplier = (long)Math.Pow(10d, (double)VisualTokenPercision);

		// ---

		/// <summary>
		/// Token in wei. Ex: 1.5 => 1500000000000000000
		/// </summary>
		public static BigInteger ToWei(decimal amount) {

			if (amount <= 0) return BigInteger.Zero;

			var left = decimal.Truncate(amount);
			var right = decimal.Round(amount - left, TokenPercision) * TokenPercisionMultiplier;

			return
				BigInteger.Multiply((BigInteger)left, (BigInteger)TokenPercisionMultiplier)
				+ (BigInteger)right
			;
		}

		/// <summary>
		/// Amount from wei. Ex: 1500000000000000000 => 1.5
		/// </summary>
		public static decimal FromWei(BigInteger amount) {

			if (amount <= 0) return 0m;

			if (amount > (BigInteger)decimal.MaxValue) {
				throw new ArgumentException("Too big value");
			}

			return (decimal)amount / TokenPercisionMultiplier;
		}

		/// <summary>
		/// Amount from wei. Ex: 1512345670000000000 => 1.512345
		/// </summary>
		public static double FromWeiFixed(BigInteger amount, bool roundUp) {
			if (amount <= 0) return 0d;

			var dec = FromWei(amount);

			var ret =
				decimal.ToDouble(decimal.Round(dec * VisualTokenPercisionMultiplier, 1))
			;

			if (!roundUp) {
				ret = Math.Floor(ret);
			}
			else {
				ret = Math.Ceiling(ret);
			}

			return ret / VisualTokenPercisionMultiplier;
		}

		// ---

		/// <summary>
		/// Fee in cents while buying gold
		/// </summary>
		public static long buyFee(BigInteger mntpBalance, long cents) {
			return 0L;
		}

		/// <summary>
		/// Fee in cents while selling gold
		/// </summary>
		public static long sellFee(BigInteger mntpBalance, long cents) {
			// 0.75%
			if (mntpBalance >= ToWei(10000)) {
				return (75L * cents / 10000L);
			}
			// 1.5%
			if (mntpBalance >= ToWei(1000)) {
				return (15L * cents / 1000L);
			}
			// 2.5%
			if (mntpBalance >= ToWei(10)) {
				return (25L * cents / 1000L);
			}
			// 3%
			return (3L * cents / 100L);
		}
	}
}
