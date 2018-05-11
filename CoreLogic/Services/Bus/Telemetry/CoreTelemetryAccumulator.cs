using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Telemetry {

	public sealed class CoreTelemetryAccumulator : BaseTelemetryAccumulator<Proto.Telemetry.CoreTelemetryMessage> {

		public CoreTelemetryAccumulator(ChildPublisher publisher, TimeSpan pubPeriod, LogFactory logFactory) : base(publisher, Topic.CoreTelemetry, new Proto.Telemetry.CoreTelemetryMessage(), pubPeriod, logFactory) {
		}
	}
}
