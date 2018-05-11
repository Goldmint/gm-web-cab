using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Telemetry {

	public sealed class WorkerTelemetryAccumulator : BaseTelemetryAccumulator<Proto.Telemetry.WorkerTelemetryMessage> {

		public WorkerTelemetryAccumulator(ChildPublisher publisher, TimeSpan pubPeriod, LogFactory logFactory) : base(publisher, Topic.CoreTelemetry, new Proto.Telemetry.WorkerTelemetryMessage(), pubPeriod, logFactory) {
		}
	}
}
