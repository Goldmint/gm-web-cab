using Goldmint.Common.Extensions;
using Google.Protobuf;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.RuntimeConfig.Impl {

	public sealed class RuntimeConfigUpdater : IDisposable, IRuntimeConfigUpdater {

		private readonly NATS.Client.IConnection _natsConnPub;
		private readonly NATS.Client.IConnection _natsConnSub;
		private readonly NATS.Client.IAsyncSubscription _natsAsyncSub;
		private readonly ILogger _logger;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

		public RuntimeConfigUpdater(Bus.IBus bus, RuntimeConfigHolder runtimeConfigHolder, ILogger logFactory) {
			_natsConnPub = bus.AllocateConnection().Result;
			_natsConnSub = bus.AllocateConnection().Result;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);

			_natsAsyncSub = _natsConnSub.SubscribeAsync(Bus.Models.Core.Pub.Subjects.ConfigUpdatedEvent);
			_natsAsyncSub.MessageHandler += OnConfigUpdated;
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_natsConnPub.Close();
			_natsConnPub.Dispose();
			_natsConnSub.Close();
			_natsConnSub.Dispose();
			_natsAsyncSub.Dispose();
		}

		// ---

		public void Run() {
			_natsAsyncSub.Start();
		}

		public void Stop() {
			_natsAsyncSub.Unsubscribe();
		}

		private void OnConfigUpdated(object sender, NATS.Client.MsgHandlerEventArgs args) {
			_runtimeConfigHolder.Reload().Wait();
		}

		public Task PublishUpdated() {
			_natsConnPub.Publish(
				Bus.Models.Core.Pub.Subjects.ConfigUpdatedEvent,
				new Bus.Models.Core.Pub.ConfigUpdatedEvent().ToByteArray()
			);
			return Task.CompletedTask;
		}
	}
}
