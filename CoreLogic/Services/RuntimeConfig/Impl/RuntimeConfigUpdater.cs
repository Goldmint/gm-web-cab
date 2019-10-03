using System;
using Goldmint.Common.Extensions;
using System.Threading.Tasks;
using Serilog;

namespace Goldmint.CoreLogic.Services.RuntimeConfig.Impl {

	public sealed class RuntimeConfigUpdater : IDisposable, IRuntimeConfigUpdater {

		private readonly NATS.Client.IConnection _natsConnPub;
		private readonly NATS.Client.IConnection _natsConnSub;
		private readonly NATS.Client.IAsyncSubscription _natsAsyncSub;
		private readonly ILogger _logger;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

		public RuntimeConfigUpdater(Bus.IConnPool bus, RuntimeConfigHolder runtimeConfigHolder, ILogger logFactory) {
			_natsConnPub = bus.GetConnection().Result;
			_natsConnSub = bus.GetConnection().Result;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);

			_natsAsyncSub = _natsConnSub.SubscribeAsync(Bus.Models.Config.Updated.Subject);
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
			_natsConnPub.Publish(Bus.Models.Config.Updated.Subject, new byte[]{ });
			return Task.CompletedTask;
		}
	}
}
