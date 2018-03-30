using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Goldmint.Common {

	public static class TextFormatter {

		private static readonly Regex _rexReplaceNonDigits = new Regex(@"[^\d]");

		public static string FormatPhoneNumber(string number) {
			number = "+" + _rexReplaceNonDigits.Replace(number, "");

			var plib = PhoneNumberUtil.GetInstance();
			var numberObj = plib.Parse(number, RegionCode.ZZ);

			if (!plib.IsValidNumber(numberObj)) throw new ArgumentException("Phone number is invalid");

			number = plib.Format(numberObj, PhoneNumberFormat.INTERNATIONAL);
			return "+" + _rexReplaceNonDigits.Replace(number, "");
		}

		public static string NormalizePhoneNumber(string number) {
			return "+" + _rexReplaceNonDigits.Replace(FormatPhoneNumber(number), "");
		}

		// ---

		// 1234.40 => 1,234.40 USD
		public static string FormatAmount(long cents, FiatCurrency currency) {
			return (cents / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " " + currency.ToString().ToUpperInvariant();
		}

		// 0x0000000000000000000000000000000000000000 => 0x000***000
		public static string MaskBlockchainAddress(string address) {
			if (string.IsNullOrWhiteSpace(address)) return "0x0";
			if (address.Length < 8) return address;
			return address.Substring(0, 5) + "***" + address.Substring(address.Length - 1 - 3, 3);
		}
	}
}
