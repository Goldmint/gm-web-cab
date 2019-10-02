using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Bus.Nats;

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public class SendTokenRequestor : BaseWorker {

		private readonly int _rowsPerRound;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public SendTokenRequestor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
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

			_dbContext.DetachEverything();

			var rows = await (
					from r in _dbContext.PoolFreezeRequest
					where
						r.Status == EmissionRequestStatus.Initial
					select r
				)
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

					var request = new MintSender.Sender.Send.Request() {
						Service = MintSender.CoreService,
						RequestID = row.Id.ToString(),
						Amount = amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
						Token = "MNT",
						PublicKey = row.SumAddress,
					};

					var msg = await _natsConn.RequestAsync(MintSender.Sender.Send.Subject, Serializer.Serialize(request), 5000);
					var rep = Serializer.Deserialize<MintSender.Sender.Send.Reply>(msg.Data);
					
					if (!rep.Success) {
						throw new Exception(rep.Error);
					}
					Logger.Information($"Emission operation #{row.Id} posted");
					success = true;
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
