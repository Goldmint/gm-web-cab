using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Nats {

	// Notification (email etc.)
	public static class Notification {

		// Enqueued
		public static class Enqueued {

			public const string Subject = "notification.enqueued";

			[ProtoContract]
			public sealed class Request {
				// DB ID
				[ProtoMember(1)]
				public long Id { get; set; }
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
