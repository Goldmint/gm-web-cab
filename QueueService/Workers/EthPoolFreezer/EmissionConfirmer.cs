using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus;
using Goldmint.DAL;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	// EmissionConfirmer receives events from mint-sender service, completing MNT emission request
	public sealed class EmissionConfirmer : BaseWorker {

		private ApplicationDbContext _dbContext;
		private IConnection _conn;

		public EmissionConfirmer(BaseOptions opts) : base(opts) {
		}

		protected override async Task OnInit(IServiceProvider services) {
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_conn = await services.GetRequiredService<IBus>().AllocateConnection();
		}

		protected override Task OnCleanup() {
			_conn.Close();
			_conn.Dispose();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			using (var sub = _conn.SubscribeAsync(
				MintSender.Subject.Sender.Event.Sent, 
				new EventHandler<MsgHandlerEventArgs>(HandleMessage))
			) {
				sub.Start();
				try { await Task.Delay(-1, base.CancellationToken); } catch (TaskCanceledException) { }
				await sub.DrainAsync();
			}
		}

		private void HandleMessage(object _, MsgHandlerEventArgs args) {
			try {
				_dbContext.DetachEverything();

				var req = MintSender.Sender.Event.Sent.Parser.ParseFrom(args.Message.Data);
				if (req.Service != "core_poolfreezer") {
					return;
				}

				// find request
				var id = long.Parse(req.Id);
				var row = (from r in _dbContext.PoolFreezeRequest where r.Id == id select r).AsTracking().LastOrDefault();
				if (row == null) {
					throw new Exception($"Row #{id} not found");
				}

				// completed
				if (row.Status == EmissionRequestStatus.Requested) {
					row.SumTransaction = req.Transaction;
					row.Status = EmissionRequestStatus.Completed;
					row.TimeCompleted = DateTime.UtcNow;
					_dbContext.SaveChanges();
								
					Logger.Information($"Emission request #{row.Id} completed");
				}

				// reply
				var rep = new MintSender.Sender.Event.SentAck() { Success = true };
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, rep.ToByteArray()
				);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to process message");
			}
		}
	}
}
