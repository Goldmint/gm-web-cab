using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.Common.Extensions {

	public static class StringExtensions {

		public static string Limit(this string v, int max) {
			if (v != null && v.Length > max) {
				return v.Substring(0, max);
			}
			return v;
		}
	}
}
