using Goldmint.CoreLogic.Services.Bus;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.SumusWallet {

	// TrackRequestor requires mint-sender service to observe new wallets for incoming transactions
	public sealed class TrackRequestor : BaseWorker {

		private readonly int _rowsPerRound;
		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IBus _bus;

		public TrackRequestor(BaseOptions opts) : base(opts) {
			_rowsPerRound = 50;
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
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
				(from r in _dbContext.UserSumusWallet where !r.Tracking select r)
				.AsTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			var request = new MintSender.Watcher.Request.AddRemove() {
				Service = "core_gold_deposit",
				Add = true,
			};
			request.PublicKey.AddRange(rows.Select(_ => _.PublicKey));

			var reply = await _bus.Request(
				MintSender.Subject.Watcher.Request.AddRemove,
				request, MintSender.Watcher.Request.AddRemoveReply.Parser
			);

			if (reply.Success) {
				foreach (var r in rows) {
					r.Tracking = true;
				}
				await _dbContext.SaveChangesAsync();
			}
		}
	}
}
