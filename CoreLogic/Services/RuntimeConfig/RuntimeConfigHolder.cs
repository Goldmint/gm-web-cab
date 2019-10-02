﻿using Goldmint.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;

namespace Goldmint.CoreLogic.Services.RuntimeConfig.Impl {

	public sealed class RuntimeConfigHolder {

		private readonly ILogger _logger;
		private readonly ReaderWriterLockSlim _locker;
		private IRuntimeConfigLoader _loader;
		private RuntimeConfig _config;

		public RuntimeConfigHolder(ILogger logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_locker = new ReaderWriterLockSlim();
			_config = new RuntimeConfig();
		}

		public RuntimeConfig Clone() {
			try {
				_locker.EnterReadLock();
				return _config.Copy();
			}
			finally {
				_locker.ExitReadLock();
			}
		}

		public void SetLoader(IRuntimeConfigLoader loader) {
			_loader = loader;
		}

		public async Task Reload() {
			_logger.Information("Reloading runtime config");

			var newConfig = Clone();

			var json = await _loader.Load();
			if (!Json.ParseInto(json, newConfig)) {
				_logger.Error("Failed to parse runtime config");
				return;
			}

			var validation = RuntimeConfig.GetValidator().Validate(newConfig);
			if (!validation.IsValid) {
				_logger.Error("Runtime config is invalid: " + validation.Errors?.FirstOrDefault());
				return;
			}

			try {
				_locker.EnterWriteLock();
				_config = newConfig;

				_logger.Information("Runtime config reloaded");
			}
			finally {
				_locker.ExitWriteLock();
			}
		}
	}
}
