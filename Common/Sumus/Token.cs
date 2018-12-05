namespace Goldmint.Common.Sumus {

	public static class Token {

		public static bool ParseToken(string s, out SumusToken token) {
			token = SumusToken.Gold;
			if (!string.IsNullOrWhiteSpace(s)) {
				s = s.ToLower();
				if (s == "0" || s == "utility" || s == "mnt" || s == "mint") {
					token = SumusToken.Mnt;
					return true;
				}
				if (s == "1" || s == "commodity" || s == "gold") {
					token = SumusToken.Gold;
					return true;
				}
			}
			return false;
		}

		public static string FormatToken(SumusToken token) {
			switch (token) {
				case SumusToken.Gold: return "GOLD";
				case SumusToken.Mnt: return "MNT";
				default: return "";
			}
		}
	}
}
