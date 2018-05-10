using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using NLog;

namespace Goldmint.QueueService.Workers.Telemetry {

	public sealed class Aggregator : BaseWorker {

		private CentralPublisher _centralPublisher;

		private readonly List<KeyValuePair<string, DefaultSubscriber>?> _subscribers;

		private readonly Dictionary<string, ApiServerStatusMessage> _dataApiServers;
		private readonly Dictionary<string, CoreServerStatusMessage> _dataCoreServers;
		private readonly ReaderWriterLockSlim _dataLocker;

		public Aggregator() {
			_subscribers = new List<KeyValuePair<string, DefaultSubscriber>?>();
			_dataLocker = new ReaderWriterLockSlim();
			_dataApiServers = new Dictionary<string, ApiServerStatusMessage>();
			_dataCoreServers = new Dictionary<string, CoreServerStatusMessage>();
		}

		protected override Task OnInit(IServiceProvider services) {

			var appConfig = services.GetRequiredService<AppConfig>();
			var logFactory = services.GetRequiredService<LogFactory>();
			_centralPublisher = services.GetRequiredService<CentralPublisher>();

			foreach (var v in appConfig.Bus.ChildPub) {
				if (string.IsNullOrWhiteSpace(v.Name)) {
					var sub = new DefaultSubscriber(
						new [] { Topic.StatusApi, Topic.StatusCore },
						new Uri(v.Endpoint),
						logFactory
					);

					sub.SetTopicCallback(Topic.StatusApi, OnStatusApi);
					sub.SetTopicCallback(Topic.StatusCore, OnStatusCore);

					_subscribers.Add(
						new KeyValuePair<string, DefaultSubscriber>(v.Name, sub)
					);
				}
			}

			foreach (var v in _subscribers) {
				v?.Value?.Run();
			}

			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			foreach (var v in _subscribers) {
				v?.Value?.Dispose();
			}
			_dataLocker?.Dispose();
			base.OnCleanup();
		}

		protected override Task OnUpdate() {
			try {

				try {
					_dataLocker.EnterReadLock();

					_centralPublisher.PublishMessage(
						Topic.StatusOverall,
						new SystemOverallStatusMessage() {
							WorkerServer = new WorkerServerStatusMessage() {
								Name = "Worker",
							},
							ApiServers = _dataApiServers.Values.ToArray(),
							CoreServers = _dataCoreServers.Values.ToArray(),
						}
					);

					Logger.Trace($"System status published");
				}
				finally {
					_dataLocker.ExitReadLock();
				}
			}
			catch (Exception e) {
				Logger.Error(e);
			}
			return Task.CompletedTask;
		}

		// ---

		private void OnStatusApi(object payload, DefaultSubscriber sub) {
			if (!(payload is ApiServerStatusMessage message)) return;

			var subData = _subscribers.FirstOrDefault(_ => _?.Value == sub);
			if (subData != null) {
				try {
					_dataLocker.EnterWriteLock();
					message.Name = subData?.Key;
					_dataApiServers[subData.Value.Key] = message;
				}
				finally {
					_dataLocker.ExitWriteLock();
				}

				Logger.Trace($"Got API server status from { subData?.Key }");
			}
		}

		private void OnStatusCore(object payload, DefaultSubscriber sub) {
			if (!(payload is CoreServerStatusMessage message)) return;

			var subData = _subscribers.FirstOrDefault(_ => _?.Value == sub);
			if (subData != null) {
				try {
					_dataLocker.EnterWriteLock();
					message.Name = subData?.Key;
					_dataCoreServers[subData.Value.Key] = message;
				}
				finally {
					_dataLocker.ExitWriteLock();
				}

				Logger.Trace($"Got core server status from { subData?.Key }");
			}
		}
	}
}
