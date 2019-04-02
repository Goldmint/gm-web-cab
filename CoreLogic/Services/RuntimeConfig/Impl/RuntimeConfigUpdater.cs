using Goldmint.Common;
using Goldmint.CoreLogic.Services.Rate.Models;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using Goldmint.Common.Extensions;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.RuntimeConfig.Impl {

	public sealed class RuntimeConfigUpdater : IDisposable, IRuntimeConfigUpdater {

		private readonly NATS.Client.IConnection _natsConnPub;
		private readonly NATS.Client.IConnection _natsConnSub;
		private readonly NATS.Client.IAsyncSubscription _natsAsyncSub;
		private readonly ILogger _logger;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

		public RuntimeConfigUpdater(NATS.Client.IConnection natsConnPub, NATS.Client.IConnection natsConnSub, RuntimeConfigHolder runtimeConfigHolder, LogFactory logFactory) {
			_natsConnPub = natsConnPub;
			_natsConnSub = natsConnSub;
			_runtimeConfigHolder = runtimeConfigHolder;
			_logger = logFactory.GetLoggerFor(this);

			_natsAsyncSub = _natsConnSub.SubscribeAsync(Bus.Nats.Config.Updated.Subject);
			_natsAsyncSub.MessageHandler += OnConfigUpdated;
		}

		public void Dispose() {
			DisposeManaged();
		}

		private void DisposeManaged() {
			_natsConnPub.Close();
			_natsConnSub.Close();
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
			_natsConnPub.Publish(Bus.Nats.Config.Updated.Subject, new byte[]{ });
			return Task.CompletedTask;
		}
	}
}
