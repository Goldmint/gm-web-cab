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
		public EthOperationsProcessor EthOpsProcessor { get; set; } = new EthOperationsProcessor();

		[ProtoMember(6)]
		public EthHarvester BuyRequestHarvester { get; set; } = new EthHarvester();

		[ProtoMember(7)]
		public EthHarvester SellRequestHarvester { get; set; } = new EthHarvester();

		[ProtoMember(8)]
		public CreditCardPaymentProcessor CreditCardVerificationPaymentProcessor { get; set; } = new CreditCardPaymentProcessor();

		[ProtoMember(9)]
		public CreditCardPaymentProcessor CreditCardRefundPaymentProcessor { get; set; } = new CreditCardPaymentProcessor();

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
		public class EthHarvester {

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
