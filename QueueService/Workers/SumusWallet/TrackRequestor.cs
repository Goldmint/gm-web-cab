using Goldmint.CoreLogic.Services.Bus.Nats;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.SumusWallet {

	public sealed class TrackRequestor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public TrackRequestor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();

			var rows = await (
				from r in _dbContext.UserSumusWallet
				where !r.Tracking
				select r
			)
			.AsTracking()
			.Take(_rowsPerRound)
			.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			var req = new Sumus.Wallet.Observe.Request() {
				Wallets = rows.Select(_ => _.PublicKey).ToArray(),
				Observe = true,
			};

			var msg = await _natsConn.RequestAsync(Sumus.Wallet.Observe.Subject, Serializer.Serialize(req), 5000);
			var rep = Serializer.Deserialize<Sumus.Wallet.Observe.Reply>(msg.Data);
			if (rep.Success) {
				foreach (var r in rows) {
					r.Tracking = true;
				}
				await _dbContext.SaveChangesAsync();
			}
		}
	}
}
