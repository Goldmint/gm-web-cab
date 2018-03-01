using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Goldmint.Common {

	public static class ValidationRules {

		public const int PasswordMinLength = 6;
		public const int PasswordMaxLength = 128;

		public static readonly Regex RexUsernameChars = new Regex("^u[0-9]+$");
		public static readonly Regex RexNameChars = new Regex("^[a-zA-Z]+$");
		public static readonly Regex RexTFAToken = new Regex("^[0-9]{6}$");
		public static readonly Regex RexLatinAndPuncts = new Regex(@"^[a-zA-Z0-9]+[a-zA-Z0-9 \-\,\.\(\)]*$");
		public static readonly Regex RexDigits = new Regex(@"^\d+$");
		public static readonly Regex RexEthereumAddress = new Regex(@"^0x[0-9abcdefABCDEF]{40}$");
		public static readonly Regex RexEthereumExchangePayload = new Regex(@"^[0-9a-zA-Z\-]{1,64}$");

		// ---

		public static bool BeIn(string x, IEnumerable<string> allowed) {
			return allowed.Contains(x);
		}

		public static bool BeValidCaptcha(string x) {
			return x != null && x.Trim().Length >= 1;
		}

		public static bool BeValidPhone(string x) {
			if (x == null || !x.StartsWith("+")) {
				return false;
			}

			try {
				var plib = PhoneNumberUtil.GetInstance();
				var numberObj = plib.Parse(x, RegionCode.ZZ);
				return plib.IsValidNumber(numberObj);
			}
			catch {
				return false;
			}
		}

		public static bool BeValidPassword(string x) {
			return x != null && x.Trim().Length >= PasswordMinLength && x.Trim().Length <= PasswordMaxLength;
		}

		public static bool BeValidEmailLength(string x) {
			return x != null && x.Trim().Length >= 5 && x.Trim().Length <= 256;
		}

		public static bool BeValidUsername(string x) {
			return x != null && x.Trim().Length >= 1 && x.Trim().Length <= 64 && RexUsernameChars.IsMatch(x);
		}

		public static bool BeValidId(long x) {
			return x > 0;
		}

		public static bool BeValidConfirmationToken(string x) {
			return x != null && x.Length >= 1;
		}

		public static bool BeValidTFACode(string x) {
			return x != null && RexTFAToken.IsMatch(x);
		}

		public static bool BeValidName(string x) {
			return x != null && RexNameChars.IsMatch(x);
		}

		public static bool BeValidDate(string x) {
			return x != null && DateTime.TryParseExact(x, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt);
		}

		public static bool BeValidCountryCodeAlpha2(string x) {
			if (x != null && x.Length == 2) {
				return Countries.IsValidAlpha2(x);
			}
			return false;
		}

		public static bool ContainLatinAndPuncts(string x) {
			return x != null && RexLatinAndPuncts.IsMatch(x);
		}

		public static bool ContainOnlyDigits(string x) {
			return x != null && RexDigits.IsMatch(x);
		}

		public static bool BeValidURL(string x) {
			return x != null && Uri.TryCreate(x, UriKind.Absolute, out var test) && (test.Scheme == "http" || test.Scheme == "https");
		}

		public static bool BeValidDectaRedirectUrl(string x) {
			return BeValidURL(x);// && x.Contains("ZZZZZZZ");
		}

		public static bool BeValidEthereumAddress(string x) {
			return x != null && RexEthereumAddress.IsMatch(x);
		}

		public static bool BeValidEthereumExchangeRequestPayload(string x) {
			return x != null && RexEthereumExchangePayload.IsMatch(x);
		}
	}
}
