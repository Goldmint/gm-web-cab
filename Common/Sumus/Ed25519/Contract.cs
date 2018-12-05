using System;

namespace Goldmint.Common.Sumus.Ed25519 {
	public static class Contract {
		public static void Requires<TException>(bool predicate) where TException : Exception, new() {
			if (!predicate) {
				throw new TException();
			}
		}
	}
}
