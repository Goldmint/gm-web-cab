using System;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;

namespace Goldmint.CoreLogicTests {

	public abstract class Test : IDisposable {

		private readonly Xunit.Abstractions.ITestOutputHelper _testOutput;
		protected NLog.LogFactory LogFactory;
		protected NLog.ILogger Logger;
		private NLog.Targets.MemoryTarget _memoryLogTarget;
		private NLog.Targets.ConsoleTarget _consoleTarget;
		protected readonly RuntimeConfigHolder RuntimeConfigHolder;
		protected readonly DebugRuntimeConfigLoader RuntimeConfigLoader;

		protected Test(Xunit.Abstractions.ITestOutputHelper testOutput) {
			_testOutput = testOutput;
			SetupNLog();
			Logger = LogFactory.GetCurrentClassLogger(this.GetType());
			RuntimeConfigHolder = new RuntimeConfigHolder(LogFactory);
			RuntimeConfigLoader = new DebugRuntimeConfigLoader();
			RuntimeConfigHolder.SetLoader(RuntimeConfigLoader);
			RuntimeConfigHolder.Reload().Wait();
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
			if (_memoryLogTarget != null) {
				foreach (var v in _memoryLogTarget.Logs) {
					_testOutput.WriteLine(v);
				}
				_memoryLogTarget?.Dispose();
			}
		}

		// ---

		private void SetupNLog() {

			var config = new NLog.Config.LoggingConfiguration();

			_memoryLogTarget = new NLog.Targets.MemoryTarget() {
				Layout = @"${uppercase:${level}}|${logger}|${message} ${exception:format=toString,Data:maxInnerExceptionLevel=100}"
			};
			config.AddTarget("memory", _memoryLogTarget);

			_consoleTarget = new NLog.Targets.ConsoleTarget() {
				Layout = @"${uppercase:${level}}|${logger}|${message} ${exception:format=toString,Data:maxInnerExceptionLevel=100}"
			};
			config.AddTarget("console", _consoleTarget);

			var ruleMemory = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, _memoryLogTarget);
			config.LoggingRules.Add(ruleMemory);

			var ruleConsole = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, _consoleTarget);
			config.LoggingRules.Add(ruleConsole);

			LogFactory = new NLog.LogFactory(config);
		}
	}
}
