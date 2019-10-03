namespace Goldmint.CoreLogic.Services.Bus.Models {

	// Runtime config update (from DB)
	public static class Config {

		// Update event
		public static class Updated {
			public const string Subject = "config.updated";
		}
	}
}
