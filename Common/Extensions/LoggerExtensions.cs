using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace Goldmint.Common.Extensions {

	public static class LoggerExtensions {

		public static ILogger GetLoggerFor(this ILogger factory, object instance) {
			var l = factory.ForContext(instance.GetType());
			return l.ForContext("type", instance.GetType().AssemblyQualifiedName);
		}

		public static ILogger GetLoggerFor(this IServiceProvider services, Type type) {
			var l = services.GetRequiredService<ILogger>().ForContext(type);
			return l.ForContext("type", type.AssemblyQualifiedName);
		}

		public static ILogger GetLoggerFor<T>(this IServiceProvider services) {
			var l = services.GetRequiredService<ILogger>().ForContext(typeof(T));
			return l.ForContext("type", typeof(T).AssemblyQualifiedName);
		}
	}
}
