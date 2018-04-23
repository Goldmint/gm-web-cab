using Goldmint.Common;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Proto {

	[ProtoContract]
	public sealed class SafeRatesMessage {

		[ProtoMember(1)]
		public SafeRate[] Rates { get; set; } = {};
	}

	[ProtoContract]
	public sealed class SafeRate {

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
