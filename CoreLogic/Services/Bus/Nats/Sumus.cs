using Goldmint.Common;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Nats {

	// Sumus is an external service for Sumus blockchain integration
	public static class Sumus {

		// Sender enqueues token sending requests
		public static class Sender {

			// Send is a command that enqueues new sending request
			public static class Send {

				public const string Subject = "sumus.sender.send";
				
				[ProtoContract]
				public sealed class Request {
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
				public sealed class Reply {
					// Success
					[ProtoMember(1)]
					public bool Success { get; set; }
					// Error description
					[ProtoMember(2)]
					public string Error { get; set; }
				}

			}

			// Sent is a callback that reports sending final status
			public static class Sent {

				public const string Subject = "sumus.sender.sent";

				[ProtoContract]
				public sealed class Request {
					// Unique request ID, length 1..64
					[ProtoMember(1)]
					public string RequestID { get; set; }
					// Success
					[ProtoMember(2)]
					public bool Success { get; set; }
					// Transaction digest in Base58 valid in case of success
					[ProtoMember(3)]
					public string Transaction { get; set; }
					// Error description
					[ProtoMember(4)]
					public string Error { get; set; }
				}

				[ProtoContract]
				public sealed class Reply {
					// Success
					[ProtoMember(1)]
					public bool Success { get; set; }
					// Error description
					[ProtoMember(2)]
					public string Error { get; set; }
				}
			}
		}
	}
}
