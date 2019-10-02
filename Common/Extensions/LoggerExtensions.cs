using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace Goldmint.Common.Extensions {

	public static class LoggerExtensions {

		public static ILogger GetLoggerFor(this ILogger factory, object instance) {
			return factory.ForContext(instance.GetType());
		}

		public static ILogger GetLoggerFor(this IServiceProvider services, Type type) {
			return services.GetRequiredService<ILogger>().ForContext(type);
		}

		public static ILogger GetLoggerFor<T>(this IServiceProvider services) {
			return services.GetRequiredService<ILogger>().ForContext(typeof(T));
		}
	}
}
