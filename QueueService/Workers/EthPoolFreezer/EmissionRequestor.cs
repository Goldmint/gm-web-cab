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

namespace Goldmint.QueueService.Workers.EthPoolFreezer {

	public class EmissionRequestor : BaseWorker {

		private readonly int _rowsPerRound;
		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public EmissionRequestor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
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

			_logger.Info($"Requesting #{rows.Length} emission operations");

			foreach (var row in rows) {
				if (IsCancelled()) return;

				var success = false;
				try {
					var request = new SumusEmitterEmitRequest() {
						RequestID = row.Id.ToString(),
						Amount = row.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
						Token = "MNT",
						Wallet = row.SumAddress,
					};

					var msg = await _natsConn.RequestAsync("sumus.emitter.emit", Serializer.Serialize(request), 5000);
					var response = Serializer.Deserialize<SumusEmitterEmitResponse>(msg.Data);
					
					if (!response.Success) {
						throw new Exception(response.Error);
					}
					_logger.Info($"Emission operation #{row.Id} posted");
					success = true;
				} catch (Exception e) {
					_logger.Error(e, $"Emission operation #{row.Id} failed to post");
				}

				if (success) {
					row.Status = EmissionRequestStatus.Requested;
					await _dbContext.SaveChangesAsync();
				}
			}
		}
	}
}
