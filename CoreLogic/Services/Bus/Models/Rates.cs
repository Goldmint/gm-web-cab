using Goldmint.Common;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Models {

	// Currency rates
	public static class Rates {

		// Rates update
		public static class Updated {

			public const string Subject = "rates.updated";

			[ProtoContract]
			public sealed class Message {
				[ProtoMember(1)]
				public Rate[] Rates { get; set; } = {};
			}

			[ProtoContract]
			public sealed class Rate {
				[ProtoMember(1)]
				public CurrencyRateType Currency { get; set; }
				[ProtoMember(2)]
				public long Stamp { get; set; }
				[ProtoMember(3)]
				public long Ttl { get; set; }
				[ProtoMember(4)]
				public bool CanBuy { get; set; }
				[ProtoMember(5)]
				public bool CanSell { get; set; }
				[ProtoMember(6)]
				public long Usd { get; set; }
			}
		}
	}
}
