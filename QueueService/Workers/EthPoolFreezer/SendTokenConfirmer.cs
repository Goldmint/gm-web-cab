using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using System.IO;
using Goldmint.CoreLogic.Services.Bus.Nats;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public class SendTokenConfirmer : BaseWorker {

		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public SendTokenConfirmer() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			using (var sub = _natsConn.SubscribeSync(MintSender.Sender.Sent.Subject)) {
				while (!IsCancelled()) {
					try {
						var msg = sub.NextMessage(1000);
						try {
							_dbContext.DetachEverything();

							// read msg
							var req = Serializer.Deserialize<MintSender.Sender.Sent.Request>(msg.Data);

							if (req.Service != MintSender.CoreService) {
								continue;
							}

							// find request
							var id = long.Parse(req.RequestID);
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

							// completed
							if (row.Status == EmissionRequestStatus.Requested) {
								row.SumTransaction = req.Transaction;
								row.Status = EmissionRequestStatus.Completed;
								row.TimeCompleted = DateTime.UtcNow;
								await _dbContext.SaveChangesAsync();
								
								Logger.Information($"Emission request #{row.Id} completed");
							}

							// reply
							var rep = new MintSender.Sender.Sent.Reply() { Success = true };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
						catch (Exception e) {
							Logger.Error(e, $"Failed to process message");
						}
					} catch{ }
				}
			}
		}
	}
}
