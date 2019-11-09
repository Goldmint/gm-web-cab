using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	// EmissionRequestor gets enqueued MNT emission requests from DB and tries to emit MNT via mint-sender service
	public sealed class EmissionRequestor : BaseWorker {

		private readonly int _rowsPerRound;
		private ApplicationDbContext _dbContext;
		private IBus _bus;

		public EmissionRequestor(BaseOptions opts) : base(opts) {
			_rowsPerRound = 50;
		}

		protected override Task OnInit(IServiceProvider services) {
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_bus = services.GetRequiredService<IBus>();
			return Task.CompletedTask;
		}

		protected override Task OnCleanup() {
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await 
				(from r in _dbContext.PoolFreezeRequest where r.Status == EmissionRequestStatus.Initial select r)
				.AsTracking()
				.OrderBy(_ => _.Id)
				.Take(_rowsPerRound)
				.ToArrayAsync()
			;

			if (IsCancelled()) return;
			if (rows.Length == 0) return;

			Logger.Information($"Requesting {rows.Length} emission operations");

			foreach (var row in rows) {
				if (IsCancelled()) return;

				var success = false;
				try {
					var amount = row.Amount;
					if (amount >= 10000m) {
						amount += 0.1m;
					}

					var request = new MintSender.Sender.Request.Send() {
						Service = "core_poolfreezer",
						Id = row.Id.ToString(),
						Amount = amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
						Token = "MNT",
						PublicKey = row.SumAddress,
					};
	
					var reply = await _bus.Request(
						MintSender.Subject.Sender.Request.Send,
						request, MintSender.Sender.Request.SendReply.Parser
					);

					if (!reply.Success) {
						throw new Exception(reply.Error);
					}
					success = true;

					Logger.Information($"Emission operation #{row.Id} posted");
				} catch (Exception e) {
					Logger.Error(e, $"Emission operation #{row.Id} failed to post");
				}

				if (success) {
					row.Status = EmissionRequestStatus.Requested;
					await _dbContext.SaveChangesAsync();
				}
			}
		}
	}
}
