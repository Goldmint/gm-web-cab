using NLog.Config;

namespace CoreLogicTests {

	public static class Test {

		public static readonly NLog.LogFactory LogFactory = new NLog.LogFactory(new LoggingConfiguration());
	}
}
