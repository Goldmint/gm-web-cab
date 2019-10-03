using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Models {

	// MintSender is an external service for Mint blockchain integration
	public static class MintSender {

		public const string CoreService = "gmcabcore";

		// Sender is a service that sends tokens on demand
		public static class Sender {

			// Send is a command that enqueues new sending request
			public static class Send {

				public const string Subject = "mint.mintsender.sender.send";
				
				[ProtoContract]
				public sealed class Request {
					// This service name
					[ProtoMember(1)]
					public string Service { get; set; }
					// Unique request ID to track later, length 1..64
					[ProtoMember(2)]
					public string RequestID { get; set; }
					// Destination wallet in Base58
					[ProtoMember(3)]
					public string PublicKey { get; set; }
					// Token, i.e. "GOLD" or "MNT"
					[ProtoMember(4)]
					public string Token { get; set; }
					// Amount in major units, i.e. "1.234" (18 decimal places)
					[ProtoMember(5)]
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

				public const string Subject = "mint.mintsender.sender.sent";

				[ProtoContract]
				public sealed class Request {
					// Success
					[ProtoMember(1)]
					public bool Success { get; set; }
					// Error description
					[ProtoMember(2)]
					public string Error { get; set; }
					// This service name
					[ProtoMember(3)]
					public string Service { get; set; }
					// Unique request ID, length 1..64
					[ProtoMember(4)]
					public string RequestID { get; set; }
					// Destination wallet in Base58
					[ProtoMember(5)]
					public string PublicKey { get; set; }
					// Token, i.e. "GOLD" or "MNT"
					[ProtoMember(6)]
					public string Token { get; set; }
					// Amount in major units, i.e. "1.234" (18 decimal places)
					[ProtoMember(7)]
					public string Amount { get; set; }
					// Transaction digest in Base58
					[ProtoMember(8)]
					public string Transaction { get; set; }
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

		// Watcher is a service that observes wallets
		public static class Watcher {

			// Watch is a command that adds/removes specified wallet to track events
			public static class Watch {

				public const string Subject = "mint.mintsender.watcher.watch";

				[ProtoContract]
				public sealed class Request {
					// This service name
					[ProtoMember(1)]
					public string Service { get; set; }
					// Wallets in Base58
					[ProtoMember(2)]
					public string[] PublicKeys { get; set; }
					// Add or remove
					[ProtoMember(3)]
					public bool Add { get; set; }
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

			// Refill is an event that describes wallet incoming transaction
			public static class Refill {

				public const string Subject = "mint.mintsender.watcher.refill";
				
				[ProtoContract]
				public sealed class Request {
					// This service name
					[ProtoMember(1)]
					public string Service { get; set; }
					// Wallet in Base58
					[ProtoMember(2)]
					public string PublicKey { get; set; }
					// From wallet in Base58
					[ProtoMember(3)]
					public string From { get; set; }
					// Token, i.e. "GOLD" or "MNT"
					[ProtoMember(4)]
					public string Token { get; set; }
					// Amount in major units, i.e. "1.234" (18 decimal places)
					[ProtoMember(5)]
					public string Amount { get; set; }
					// Transaction hash in Base58
					[ProtoMember(6)]
					public string Transaction { get; set; }
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
