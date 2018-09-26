using System;
using System.Numerics;

namespace Goldmint.Common.Sumus {

	public class Amount {

		public TokenType? Token { get; set; }
		public BigInteger Value { get; set; }

		public Amount() {
			Token = null;
			Value = new BigInteger(0);
		}

		public Amount(TokenType token) {
			Token = token;
			Value = new BigInteger(0);
		}

		public Amount(BigInteger amount, TokenType? token = null) {
			Token = token;
			Value = amount;
		}

		public Amount(string floats, TokenType? token = null) {
			Token = token;
			Value = new BigInteger(0);
			try {
				var neg = floats.StartsWith('-');
				floats = floats.TrimStart('-');
				if (floats.Contains(".")) {
					var p = floats.Split('.', 2);
					Value = BigInteger.Parse(p[0]) * BigInteger.Pow(new BigInteger(10), 18) + BigInteger.Parse(p[1].PadRight(18, '0'));
				}
				else {
					Value = BigInteger.Parse(floats) * BigInteger.Pow(new BigInteger(10), 18);
				}

				if (neg) {
					Value *= BigInteger.MinusOne;
				}
			}
			catch {
				throw new ArgumentException("Failed to parse amount");
			}
		}

		// ---

		public override string ToString() {
			if (Value == 0) return "0";
			var str = Value.ToString().TrimStart('-').PadLeft(19, '0');
			str = str.Substring(0, str.Length - 18) + "." + str.Substring(str.Length - 18);
			return (Value < 0? "-":"") + str;
		}

		public string ToString(bool printToken) {
			return ToString() + (!printToken || Token == null ? "" : " " + FormatToken(Token.Value));
		}

		// ---

		public enum TokenType : ushort {
			Mnt = 0,
			Gold = 1,
		}

		public static bool ParseToken(string s, out TokenType token) {
			token = TokenType.Gold;
			if (!string.IsNullOrWhiteSpace(s)) {
				s = s.ToLower();
				if (s == "0" || s == "utility" || s == "mnt" || s == "mint") {
					token = TokenType.Mnt;
					return true;
				}
				if (s == "1" || s == "commodity" || s == "gold") {
					token = TokenType.Gold;
					return true;
				}
			}
			return false;
		}

		public static string FormatToken(TokenType token) {
			switch (token) {
				case TokenType.Gold: return "GOLD";
				case TokenType.Mnt: return "MNT";
				default: return "";
			}
		}
	}
}
