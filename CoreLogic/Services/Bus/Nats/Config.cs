using Goldmint.Common;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Nats {

	// Runtime config update (from DB)
	public static class Config {

		// Update event
		public static class Updated {
			public const string Subject = "config.updated";
		}
	}
}
