using System;
using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Proto.Telemetry {

	/// <summary>
	/// API telemetry
	/// </summary>
	[ProtoContract]
	public sealed class ApiTelemetryMessage {

		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public string StartupTime { get; set; } = DateTime.UtcNow.ToString("R");

		[ProtoMember(3)]
		public string RuntimeConfigStamp { get; set; }

		[ProtoMember(4)]
		public SafeRates.SafeRatesMessage RatesData { get; set; }
	}

	/// <summary>
	/// Core telemetry
	/// </summary>
	[ProtoContract]
	public sealed class CoreTelemetryMessage {

		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public string StartupTime { get; set; } = DateTime.UtcNow.ToString("R");

		[ProtoMember(3)]
		public string RuntimeConfigStamp { get; set; }

		[ProtoMember(4)]
		public SafeRates.SafeRatesMessage RatesData { get; set; }

		[ProtoMember(5)]
		public EthOperationsProcessor EthereumOperations { get; set; } = new EthOperationsProcessor();

		[ProtoMember(6)]
		public EthEventHarvester ContractBuyEvents { get; set; } = new EthEventHarvester();

		[ProtoMember(7)]
		public EthEventHarvester ContractSellEvents { get; set; } = new EthEventHarvester();

		[ProtoMember(8)]
		public CreditCardPaymentProcessor CreditCardVerifications { get; set; } = new CreditCardPaymentProcessor();

		[ProtoMember(9)]
		public CreditCardPaymentProcessor CreditCardRefunds { get; set; } = new CreditCardPaymentProcessor();

		[ProtoMember(10)]
		public CreditCardPaymentProcessor CreditCardDespoits { get; set; } = new CreditCardPaymentProcessor();

		[ProtoMember(11)]
		public CreditCardPaymentProcessor CreditCardWithdrawals { get; set; } = new CreditCardPaymentProcessor();

		[ProtoMember(12)]
		public EthEventHarvester PoolFreezerEvents { get; set; } = new EthEventHarvester();

		// ---

		[ProtoContract]
		public class EthOperationsProcessor {

			[ProtoMember(1)]
			public int Load { get; set; }

			[ProtoMember(2)]
			public long Exceptions { get; set; }

			[ProtoMember(3)]
			public long ProcessedSinceStartup { get; set; }

			[ProtoMember(4)]
			public long FailedSinceStartup { get; set; }
		}

		[ProtoContract]
		public class EthEventHarvester {

			[ProtoMember(1)]
			public int Load { get; set; }

			[ProtoMember(2)]
			public long Exceptions { get; set; }

			[ProtoMember(3)]
			public string LastBlock { get; set; }

			[ProtoMember(4)]
			public int StepBlocks { get; set; }

			[ProtoMember(5)]
			public long ProcessedSinceStartup { get; set; }

			[ProtoMember(6)]
			public long ConfirmationsRequired { get; set; }
		}

		[ProtoContract]
		public class CreditCardPaymentProcessor {

			[ProtoMember(1)]
			public int Load { get; set; }

			[ProtoMember(2)]
			public long Exceptions { get; set; }

			[ProtoMember(3)]
			public long ProcessedSinceStartup { get; set; }

			[ProtoMember(4)]
			public long FailedSinceStartup { get; set; }
		}
	}

	/// <summary>
	/// Worker telemetry
	/// </summary>
	[ProtoContract]
	public sealed class WorkerTelemetryMessage {

		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public string StartupTime { get; set; } = DateTime.UtcNow.ToString("R");

		[ProtoMember(3)]
		public string RuntimeConfigStamp { get; set; }

		[ProtoMember(4)]
		public SafeRates.SafeRatesMessage RatesData { get; set; }
	}

	/// <summary>
	/// Aggregated telemetry
	/// </summary>
	[ProtoContract]
	public sealed class AggregatedTelemetryMessage {

		[ProtoMember(1)]
		public OnlineStatus[] Online { get; set; }

		[ProtoMember(2)]
		public ApiTelemetryMessage[] ApiServers { get; set; }

		[ProtoMember(3)]
		public WorkerTelemetryMessage[] WorkerServers { get; set; }

		[ProtoMember(4)]
		public CoreTelemetryMessage[] CoreServers { get; set; }

		// ---

		[ProtoContract]
		public class OnlineStatus {

			[ProtoMember(1)]
			public string Name { get; set; }

			[ProtoMember(2)]
			public bool Up { get; set; }
		}
	}
}
