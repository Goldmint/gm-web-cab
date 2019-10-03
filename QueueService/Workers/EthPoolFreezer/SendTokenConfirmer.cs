using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Bus.Models;
using Goldmint.CoreLogic.Services.Bus;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public class SendTokenConfirmer : BaseWorker {

		private ApplicationDbContext _dbContext;
		private IConnPool _bus;

		public SendTokenConfirmer() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_bus = services.GetRequiredService<IConnPool>();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			using (var conn = await _bus.GetConnection()) {
				using (var sub = conn.SubscribeSync(MintSender.Sender.Sent.Subject)) {
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
								conn.Publish(msg.Reply, Serializer.Serialize(rep));
							}
							catch (Exception e) {
								Logger.Error(e, $"Failed to process message");
							}
						} catch{ }
					}
				}
				conn.Close();
			}
		}
	}
}
