﻿namespace Goldmint.CoreLogic.Services.KYC {

	public sealed class CallbackResult {

		public VerificationStatus OverallStatus { get; set; }
		public string TicketId { get; set; }
		public string ServiceStatus { get; set; }
		public string ServiceMessage { get; set; }
	}
}
