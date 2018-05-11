using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Telemetry {

	public sealed class ApiTelemetryAccumulator : BaseTelemetryAccumulator<Proto.Telemetry.ApiTelemetryMessage> {

		public ApiTelemetryAccumulator(ChildPublisher publisher, TimeSpan pubPeriod, LogFactory logFactory) : base(publisher, Topic.ApiTelemetry, new Proto.Telemetry.ApiTelemetryMessage(), pubPeriod, logFactory) {
		}
	}
}
