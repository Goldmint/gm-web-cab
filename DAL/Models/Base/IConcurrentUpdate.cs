using System;

namespace Goldmint.DAL.Models {

	public interface IConcurrentUpdate {

		void OnConcurrencyStampRegen();
	}

	public static class ConcurrentStamp {

		public static string GetGuid() {
			return Guid.NewGuid().ToString("N");
		}
	}
}
