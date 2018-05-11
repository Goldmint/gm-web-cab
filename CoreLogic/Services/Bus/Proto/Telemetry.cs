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
		public EthHarvester BuyRequestHarvester { get; set; } = new EthHarvester();

		[ProtoMember(4)]
		public EthHarvester SellRequestHarvester { get; set; } = new EthHarvester();

		// ---

		[ProtoContract]
		public class EthHarvester {

			[ProtoMember(1)]
			public string LastBlock { get; set; }

			[ProtoMember(2)]
			public int StepBlocks { get; set; }

			[ProtoMember(3)]
			public long ProcessedSinceStartup { get; set; }

			[ProtoMember(4)]
			public long ConfirmationsRequired { get; set; }
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
