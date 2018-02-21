namespace Goldmint.CoreLogic.Services.SignedDoc {

	public sealed class CallbackResult {

		public OverallStatus OverallStatus { get; set; }
		public string ReferenceId { get; set; }
		public string ServiceStatus { get; set; }
		public string ServiceMessage { get; set; }
	}

	public enum OverallStatus {
		Pending,
		Signed,
		Declined,
		Error
	}
}
