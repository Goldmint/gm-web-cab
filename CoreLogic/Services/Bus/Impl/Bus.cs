using Goldmint.Common;
using Goldmint.Common.Extensions;
using Google.Protobuf;
using NATS.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Bus.Impl {

	public sealed class Bus : IDisposable, IBus {

		private readonly ILogger _logger;
		private readonly ConnectionFactory _factory;
		private readonly Options _options;

		private readonly int _allocatedMax;
		private int _allocated;
		private readonly Queue<IConnection> _pool;
		private readonly object _poolLock;
		private readonly AutoResetEvent _waiter;

		public Bus(AppConfig appConfig, ILogger logFactory) {
			_logger = logFactory.GetLoggerFor(this);

			_waiter = new AutoResetEvent(false);
			_allocatedMax = Math.Max(1, appConfig.Bus.Nats.PoolSize);
			_allocated = 0;
			_pool = new Queue<IConnection>();
			_poolLock = new object();

			_factory = new ConnectionFactory();
			_options = ConnectionFactory.GetDefaultOptions();
			{
				_options.Url = appConfig.Bus.Nats.Endpoint;
				_options.AllowReconnect = true;
				_options.Timeout = 5000;
			}
		}
		
		public void Dispose() {
			_waiter.Dispose();
			foreach (var c in _pool) {
				c.Close();
				c.Dispose();
			}
		}
		
		public Task<IConnection> AllocateConnection() {
			return Task.FromResult(_factory.CreateConnection(_options));
		}

		public Task<IConnection> RequireConnection(int timeoutMs) {
			while (true) {
				lock (_poolLock) {
					if (_pool.Count > 0) {
						return Task.FromResult(_pool.Dequeue());
					}
					if (_allocated < _allocatedMax) {
						_allocated++;
						return Task.FromResult(_factory.CreateConnection(_options));
					}
				}
				if (!_waiter.WaitOne(timeoutMs)) {
					throw new TimeoutException();
				}
			}
		}

		public Task ReleaseConnection(IConnection c) {
			if (c == null || c.IsClosed() || c.IsDraining()) {
				throw new ArgumentException("could not release null, closed or draining connection");
			}
			lock (_poolLock) {
				_pool.Enqueue(c);
				_waiter.Set();
			}
			return Task.CompletedTask;
		}

		public async Task<Trep> Request<Treq, Trep>(string subject, Treq request, MessageParser<Trep> replyParser, int timeoutMs) where Treq : IMessage where Trep : IMessage<Trep> {
			IConnection conn = null;
			try {
				conn = await RequireConnection(timeoutMs);
				var msg = await conn.RequestAsync(subject, request.ToByteArray(), timeoutMs);
				return replyParser.ParseFrom(msg.Data);
			} finally {
				await ReleaseConnection(conn);
			}
		}
	}
}
