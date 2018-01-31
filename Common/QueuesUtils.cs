using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.Common {

	public static class QueuesUtils {

		/// <summary>
		/// Resolves next delay depending on current time and time of entity creation
		/// </summary>
		public static TimeSpan GetNextCheckDelay(DateTime entityCreationTime, TimeSpan defaultDelay, int rounds) {
			var nextCheckDelay = defaultDelay;
			
			if (DateTime.UtcNow > entityCreationTime) {
				rounds = Math.Max(1, rounds);

				var timeSinceCreation = (long)(DateTime.UtcNow - entityCreationTime).TotalSeconds;
				var defaultPeriod = (long)defaultDelay.TotalSeconds;
				nextCheckDelay *= Math.Max(1L, timeSinceCreation / defaultPeriod / rounds);
			}

			return nextCheckDelay;
		}

	}
}
