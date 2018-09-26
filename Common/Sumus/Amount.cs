using System.Numerics;

namespace Goldmint.Common.Sumus {

	public class Amount {

		public TokenType Token { get; set; }
		public BigInteger Value { get; set; }

		public Amount() {
			Token = TokenType.Gold;
			Value = new BigInteger(0);
		}

		public Amount(TokenType token) {
			Token = token;
			Value = new BigInteger(0);
		}

		public Amount(BigInteger amount) {
			Token = TokenType.Gold;
			Value = amount;
		}

		// ---

		public override string ToString() {
			const int tokenDecimals = 18;
			if (Value <= 0) return "0";
			var str = Value.ToString().PadLeft(tokenDecimals + 1, '0');
			str = str.Substring(0, str.Length - tokenDecimals) + "." + str.Substring(str.Length - tokenDecimals);
			return str.TrimEnd('0', '.');
		}

		public string ToString(bool printToken) {
			return ToString() + (!printToken ? "" : " " + FormatToken(Token));
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
