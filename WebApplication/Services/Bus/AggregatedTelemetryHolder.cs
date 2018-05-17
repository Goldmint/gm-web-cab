using Goldmint.CoreLogic.Services.Bus.Proto.Telemetry;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using System;
using System.Threading;
using Goldmint.Common;

namespace Goldmint.WebApplication.Services.Bus {

	public sealed class AggregatedTelemetryHolder : IDisposable {

		private readonly ReaderWriterLockSlim _locker;
		private readonly TimeSpan _becomeStaleAfter;
		private AggregatedTelemetryMessage _data;
		private DateTime _lastUpdate;
		private string _dataJson;

		public AggregatedTelemetryHolder(AppConfig appConfig) {
			_locker = new ReaderWriterLockSlim();
			_lastUpdate = DateTime.MinValue;
			_becomeStaleAfter = TimeSpan.FromSeconds(30);
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
					_dataJson = Common.Json.Stringify(_data);
					_lastUpdate = DateTime.UtcNow;
				}
				finally {
					_locker.ExitWriteLock();
				}
			}
		}

		public string GetJson() {
			try {
				_locker.EnterReadLock();
				if (DateTime.UtcNow - _lastUpdate > _becomeStaleAfter) {
					return "{}";
				}
				return _dataJson;
			}
			finally {
				_locker.ExitReadLock();
			}
		}
	}
}
