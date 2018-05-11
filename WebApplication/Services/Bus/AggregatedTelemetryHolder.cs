using Goldmint.CoreLogic.Services.Bus.Proto.Telemetry;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using System;
using System.Threading;

namespace Goldmint.WebApplication.Services.Bus {

	public sealed class AggregatedTelemetryHolder : IDisposable {

		private readonly ReaderWriterLockSlim _locker;
		private AggregatedTelemetryMessage _data;
		private string _dataJson;

		public AggregatedTelemetryHolder() {
			_locker = new ReaderWriterLockSlim();
			_data = null;
			_dataJson = "{}";
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_locker?.Dispose();
		}

		public void OnUpdate(object payload, DefaultSubscriber sub) {
			if (payload is AggregatedTelemetryMessage data) {
				try {
					_locker.EnterWriteLock();
					_data = data;
					_dataJson = _data == null? "{}": Common.Json.Stringify(_data);
				}
				finally {
					_locker.ExitWriteLock();
				}
			}
		}

		public string GetJson() {
			return _dataJson;
		}
	}
}
