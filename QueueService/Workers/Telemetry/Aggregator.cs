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

		private readonly Dictionary<ServerInfo, DefaultSubscriber> _subscribers;
		private readonly ReaderWriterLockSlim _locker;

		public Aggregator() {
			_locker = new ReaderWriterLockSlim();
			_subscribers = new Dictionary<ServerInfo, DefaultSubscriber>();
		}

		protected override Task OnInit(IServiceProvider services) {

			var appConfig = services.GetRequiredService<AppConfig>();
			var logFactory = services.GetRequiredService<LogFactory>();
			_centralPublisher = services.GetRequiredService<CentralPublisher>();

			foreach (var v in appConfig.Bus.ChildPub) {
				if (string.IsNullOrWhiteSpace(v.Name)) {
					var sub = new DefaultSubscriber(
						new[] { Topic.StatusApi, Topic.StatusCore },
						new Uri(v.Endpoint),
						logFactory
					);

					sub.SetTopicCallback(Topic.StatusApi, (p, s) => {
						if (!(p is ApiServerStatusMessage msg)) return;
						OnStatus(p, s);
					});
					sub.SetTopicCallback(Topic.StatusCore, (p, s) => {
						if (!(p is CoreServerStatusMessage msg)) return;
						OnStatus(p, s);
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

					_centralPublisher.PublishMessage(
						Topic.StatusOverall,
						new SystemOverallStatusMessage() {

							Online = _subscribers.Select(_ => new ServerOnlineMessage() {
								Name = _.Key.Name,
								Up = _.Key.LastStatus != null && DateTime.UtcNow - _.Key.LastStatus.Value < TimeSpan.FromSeconds(30),
							}).ToArray(),

							WorkerServer = new WorkerServerStatusMessage() {
								Name = "Worker",
							},
							ApiServers = _subscribers.Where(_ => _.Key.Message is ApiServerStatusMessage).Select(_ => _.Key.Message as ApiServerStatusMessage).ToArray(),
							CoreServers = _subscribers.Where(_ => _.Key.Message is CoreServerStatusMessage).Select(_ => _.Key.Message as CoreServerStatusMessage).ToArray(),
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

		private void OnStatus(object payload, DefaultSubscriber sub) {
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
		
		// ---

		internal class ServerInfo {

			public string Name { get; set; }
			public DateTime? LastStatus { get; set; }
			public object Message { get; set; }
		}
	}
}
