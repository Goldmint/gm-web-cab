using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;

namespace Goldmint.Common {

	public static class LoggerExtensions {

		public static ILogger GetLoggerFor(this LogFactory factory, object instance) {
			return factory.GetLogger(instance.GetType().FullName);
		}

		public static ILogger GetLoggerFor(this IServiceProvider services, Type type) {
			return services.GetRequiredService<LogFactory>().GetLogger(type.FullName);
		}

		public static ILogger GetLoggerFor<T>(this IServiceProvider services) {
			return services.GetRequiredService<LogFactory>().GetLogger(typeof(T).FullName);
		}
	}
}
