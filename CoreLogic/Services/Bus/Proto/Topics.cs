namespace Goldmint.CoreLogic.Services.Bus.Proto {

	public enum Topic {
		Unknown,

		Hb,
		FiatRates,
		ConfigUpdated,

		ApiTelemetry,
		CoreTelemetry,
		WorkerTelemetry,
		AggregatedTelemetry
	}
}
