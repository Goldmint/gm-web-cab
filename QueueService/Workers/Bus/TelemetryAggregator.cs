using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Proto.Config;
using Goldmint.CoreLogic.Services.Bus.Proto.Telemetry;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using Goldmint.CoreLogic.Services.Bus.Telemetry;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using NLog;

namespace Goldmint.QueueService.Workers.Bus {

	public sealed class TelemetryAggregator : BaseWorker {

		private RuntimeConfigHolder _runtimeConfigHolder;
		private CentralPublisher _centralPublisher;
		//private WorkerTelemetryMessage _selfTelemetryMessage;
		private WorkerTelemetryAccumulator _thisWorkerTelemetryAccumulator;

		private readonly Dictionary<ServerInfo, DefaultSubscriber> _subscribers;
		private readonly ReaderWriterLockSlim _locker;

		public TelemetryAggregator() {
			_locker = new ReaderWriterLockSlim();
			_subscribers = new Dictionary<ServerInfo, DefaultSubscriber>();
			
			/*_selfTelemetryMessage = new WorkerTelemetryMessage() {
				Name = "Telemetry aggregator (worker)",
			};*/
		}

		protected override Task OnInit(IServiceProvider services) {

			var appConfig = services.GetRequiredService<AppConfig>();
			var logFactory = services.GetRequiredService<LogFactory>();
			_runtimeConfigHolder = services.GetRequiredService<RuntimeConfigHolder>();
			_centralPublisher = services.GetRequiredService<CentralPublisher>();
			_thisWorkerTelemetryAccumulator = services.GetRequiredService<WorkerTelemetryAccumulator>();

			_thisWorkerTelemetryAccumulator.AccessData(_ => _.Name = "Aggregator-worker");

			foreach (var v in appConfig.Bus.CentralPub.ChildPubEndpoints) {
				if (!string.IsNullOrWhiteSpace(v.Name)) {
					var sub = new DefaultSubscriber(
						new[] { Topic.ConfigUpdated, Topic.ApiTelemetry, Topic.CoreTelemetry, Topic.WorkerTelemetry },
						new Uri(v.Endpoint),
						logFactory
					);

					sub.SetTopicCallback(Topic.ApiTelemetry, (p, s) => {
						if (!(p is ApiTelemetryMessage msg)) return;
						OnFreshTelemetry(p, s);
					});
					sub.SetTopicCallback(Topic.CoreTelemetry, (p, s) => {
						if (!(p is CoreTelemetryMessage msg)) return;
						OnFreshTelemetry(p, s);
					});
					sub.SetTopicCallback(Topic.WorkerTelemetry, (p, s) => {
						if (!(p is WorkerTelemetryMessage msg)) return;
						OnFreshTelemetry(p, s);
					});
					sub.SetTopicCallback(Topic.ConfigUpdated, (p, s) => {
						if (!(p is ConfigUpdatedMessage msg)) return;
						OnConfigUpdated(msg, s);
					});

					_subscribers.Add(
						new ServerInfo() {
							Name = v.Name,
							LastStatus = null,
							Message = null,
						},
						sub
					);
				}
			}

			foreach (var v in _subscribers) {
				v.Value?.Run();
			}

			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			foreach (var v in _subscribers) {
				v.Value?.Dispose();
			}
			_locker?.Dispose();
			base.OnCleanup();
		}

		protected override Task OnUpdate() {
			try {

				try {
					_locker.EnterReadLock();

					var thisWorkerTelemetry = _thisWorkerTelemetryAccumulator.CloneData();

					_centralPublisher.PublishMessage(
						Topic.AggregatedTelemetry,
						new AggregatedTelemetryMessage() {

							Online = _subscribers.Select(_ => new AggregatedTelemetryMessage.OnlineStatus() {
								Name = _.Key.Name,
								Up = _.Key.LastStatus != null && DateTime.UtcNow - _.Key.LastStatus.Value < TimeSpan.FromSeconds(30),
							}).ToArray(),

							ApiServers = _subscribers.Where(_ => _.Key.Message is ApiTelemetryMessage).Select(_ => {
								var r = _.Key.Message as ApiTelemetryMessage;
								r.Name = _.Key.Name;
								return r;
							}).ToArray(),

							WorkerServers = _subscribers.Where(_ => _.Key.Message is WorkerTelemetryMessage).Select(_ => {
								var r = _.Key.Message as WorkerTelemetryMessage;
								r.Name = _.Key.Name;
								return r;
							}).Append(thisWorkerTelemetry).ToArray(),

							CoreServers = _subscribers.Where(_ => _.Key.Message is CoreTelemetryMessage).Select(_ => {
								var r =_.Key.Message as CoreTelemetryMessage;
								r.Name = _.Key.Name;
								return r;
							}).ToArray(),
						}
					);

					Logger.Trace($"System status published");
				}
				finally {
					_locker.ExitReadLock();
				}
			}
			catch (Exception e) {
				Logger.Error(e);
			}
			return Task.CompletedTask;
		}

		// ---

		private void OnFreshTelemetry(object payload, DefaultSubscriber sub) {
			try {
				_locker.EnterWriteLock();

				var subData = _subscribers.FirstOrDefault(_ => _.Value == sub).Key;
				if (subData != null) {

					subData.LastStatus = DateTime.UtcNow;
					subData.Message = payload;

					Logger.Trace($"Got server status from { subData.Name }");
				}
			}
			finally {
				_locker.ExitWriteLock();
			}
		}

		private void OnConfigUpdated(ConfigUpdatedMessage msg, DefaultSubscriber sub) {

			var apply = false;
			try {
				_locker.EnterWriteLock();

				var subData = _subscribers.FirstOrDefault(_ => _.Value == sub).Key;
				if (subData != null) {

					Logger.Warn($"User { msg.Username } has modified runtime config. Received from { subData.Name }. Reloading and broadcasting..");
					apply = true;
				}
			}
			finally {
				_locker.ExitWriteLock();
			}

			if (apply) {

				Task.Factory.StartNew(async () => {
					await _runtimeConfigHolder.Reload();
					var rcfg = _runtimeConfigHolder.Clone();
					_thisWorkerTelemetryAccumulator.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);
				});

				// broadcast
				_centralPublisher.PublishMessage(
					Topic.ConfigUpdated,
					msg
				);
			}
		}

		// ---

		internal class ServerInfo {

			public string Name { get; set; }
			public DateTime? LastStatus { get; set; }
			public object Message { get; set; }
		}
	}
}
