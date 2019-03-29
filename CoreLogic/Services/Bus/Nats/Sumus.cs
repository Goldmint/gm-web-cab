using Goldmint.Common;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Nats {

	[ProtoContract]
	public sealed class SumusEmitterEmitRequest {

		// Unique request ID to track later, length 1..64
		[ProtoMember(1)]
		public string RequestID { get; set; }

		// Wallet in Base58
		[ProtoMember(2)]
		public string Wallet { get; set; }

		// Token, i.e. "GOLD" or "MNT"
		[ProtoMember(3)]
		public string Token { get; set; }

		// Amount in major units, i.e. "1.234" (18 decimal places)
		[ProtoMember(4)]
		public string Amount { get; set; }
	}

	[ProtoContract]
	public sealed class SumusEmitterEmitResponse {

		// Success
		[ProtoMember(1)]
		public bool Success { get; set; }

		// Error description
		[ProtoMember(2)]
		public string Error { get; set; }
	}

	// ---

	[ProtoContract]
	public sealed class SumusEmitterEmitedRequest {

		// Unique request ID to track later, length 1..64
		[ProtoMember(1)]
		public string RequestID { get; set; }

		// Transaction digest in Base58
		[ProtoMember(2)]
		public string Transaction { get; set; }
	}

	[ProtoContract]
	public sealed class SumusEmitterEmitedResponse {

		// Success
		[ProtoMember(1)]
		public bool Success { get; set; }

		// Error description
		[ProtoMember(2)]
		public string Error { get; set; }
	}
}
