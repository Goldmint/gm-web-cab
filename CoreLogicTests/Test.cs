using System;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Serilog;

namespace Goldmint.CoreLogicTests {

	public abstract class Test : IDisposable {

		private readonly Xunit.Abstractions.ITestOutputHelper _testOutput;
		protected readonly RuntimeConfigHolder RuntimeConfigHolder;
		protected readonly DebugRuntimeConfigLoader RuntimeConfigLoader;

		protected Test(Xunit.Abstractions.ITestOutputHelper testOutput) {
			_testOutput = testOutput;
			Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
			RuntimeConfigHolder = new RuntimeConfigHolder(Log.Logger);
			RuntimeConfigLoader = new DebugRuntimeConfigLoader();
			RuntimeConfigHolder.SetLoader(RuntimeConfigLoader);
			RuntimeConfigHolder.Reload().Wait();
		}

		public void Dispose() {
			DisposeManaged();
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() {
		}
	}
}
