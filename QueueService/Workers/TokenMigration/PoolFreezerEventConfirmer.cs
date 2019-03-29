using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using System.IO;
using Goldmint.CoreLogic.Services.Bus.Nats;

namespace Goldmint.QueueService.Workers.TokenMigration {

	public class PoolFreezerEventConfirmer : BaseWorker {

		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public PoolFreezerEventConfirmer() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			using (var sub = _natsConn.SubscribeSync("sumus.emitter.emited")) {
				while (!IsCancelled()) {
					try {
						var msg = sub.NextMessage(1000);
						try {
							_logger.Trace($"Got message");

							var request = Serializer.Deserialize<SumusEmitterEmitedRequest>(msg.Data);
							var response = new SumusEmitterEmitedResponse() {
								Success = true,
							};

							_dbContext.DetachEverything();
							var id = long.Parse(request.RequestID);

							var row = await (
								from r in _dbContext.PoolFreezeRequest
								where
									r.Id == id
								select r
							)
							.AsTracking()
							.LastAsync();

							if (row == null) {
								throw new Exception($"Row #{id} not found");
							}

							if (row.Status == EmissionRequestStatus.Requested) {
								row.SumTransaction = request.Transaction;
								row.Status = EmissionRequestStatus.Completed;
								row.TimeCompleted = DateTime.UtcNow;
								await _dbContext.SaveChangesAsync();
								_logger.Trace($"Row #{id} status completed");
							}

							_natsConn.Publish(msg.Reply, Serializer.Serialize(response));
						}
						catch (Exception e) {
							_logger.Error(e, $"Failed to process message");
						}
					} catch{ }
				}
			}
		}
	}
}
