using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Proto {

	[ProtoContract]
	public sealed class SafeRates {

		[ProtoMember(1)]
		public long Stamp { get; set; }

		[ProtoMember(2)]
		public Gold Gold { get; set; }

		[ProtoMember(3)]
		public Crypto Crypto { get; set; }
	}

	[ProtoContract]
	public sealed class Gold {

		[ProtoMember(1)]
		public bool IsSafeForBuy { get; set; }

		[ProtoMember(2)]
		public bool IsSafeForSell { get; set; }

		[ProtoMember(3)]
		public long Usd { get; set; }

		// ...
	}

	[ProtoContract]
	public sealed class Crypto {

		[ProtoMember(1)]
		public bool IsSafeForBuy { get; set; }

		[ProtoMember(2)]
		public bool IsSafeForSell { get; set; }

		[ProtoMember(3)]
		public long EthUsd { get; set; }

		// ...
	}
}
