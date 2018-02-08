using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Goldmint.CoreLogic.Finance.Tokens {

	public static class MntpToken {

		public static readonly int TokenPercision = 18;
		public static readonly decimal TokenPercisionMultiplier = (long)Math.Pow(10d, (double)TokenPercision);

		public static readonly int VisualTokenPercision = 6;
		public static readonly long VisualTokenPercisionMultiplier = (long)Math.Pow(10d, (double)VisualTokenPercision);

		// ---

		/// <summary>
		/// Token in wei. Ex: 1.5 => 1500000000000000000
		/// </summary>
		public static BigInteger ToWei(decimal amount) {
			if (amount <= 0) return BigInteger.Zero;
			var str = amount.ToString("F" + (TokenPercision + 1), System.Globalization.CultureInfo.InvariantCulture);
			var parts = str.Substring(0, str.Length - 1).Split('.');
			var left = parts.ElementAtOrDefault(0);
			var right = (parts.ElementAtOrDefault(1) ?? "0");
			return BigInteger.Parse(left + right);
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

		// ---

		/// <summary>
		/// Fee in cents while buying gold
		/// </summary>
		public static long getBuyingFee(BigInteger mntpBalance, long cents) {
			if (cents < 1) {
				return 0L;
			}

			return 0L;
		}

		/// <summary>
		/// Fee in cents while selling gold
		/// </summary>
		public static long getSellingFee(BigInteger mntpBalance, long cents) {

			if (cents < 1) {
				return 0L;
			}

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
