using Goldmint.Common;
using Goldmint.Common.Extensions;
using Serilog;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Impl {

	public sealed class ConnPool : IConnPool {

		private readonly ILogger _logger;
		private readonly NATS.Client.ConnectionFactory _factory;
		private readonly NATS.Client.Options _options;

		public ConnPool(AppConfig appConfig, ILogger logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_factory = new NATS.Client.ConnectionFactory();
			_options = NATS.Client.ConnectionFactory.GetDefaultOptions();
			{
				_options.Url = appConfig.Bus.Nats.Endpoint;
				_options.AllowReconnect = true;
				_options.Timeout = 5000;
			}
		}

		public Task<NATS.Client.IConnection> GetConnection() {
			return Task.FromResult(_factory.CreateConnection(_options));
		}
	}
}
