using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.Sell {

	public sealed class RequestProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public RequestProcessor(int rowsPerRound) {
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
				from r in _dbContext.SellGoldEth
				where r.Status == SellGoldRequestStatus.Confirmed
				select r
			)
			.AsTracking()
			.Take(_rowsPerRound)
			.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var r in rows) {
				if (IsCancelled()) return;
				
				// TODO: check sender ether balance; stop otherwise

				try {
					using (var tx = await _dbContext.Database.BeginTransactionAsync()) {
						
						r.Status = SellGoldRequestStatus.Success;
						r.TimeCompleted = DateTime.UtcNow;
						_dbContext.SaveChanges();

						var sending = new DAL.Models.EthSending() {
							Status = EthereumOperationStatus.Initial,
							Amount = r.EthAmount,
							Address = r.Destination,
							TimeCreated = DateTime.UtcNow,
							TimeNextCheck = DateTime.UtcNow,
							OplogId = r.OplogId,
							RelUserFinHistoryId = r.RelUserFinHistoryId,
							UserId = r.UserId,
						};
						_dbContext.EthSending.Add(sending);
						_dbContext.SaveChanges();

						tx.Commit();
					}

				} catch (Exception e) {
					Logger.Error(e, $"Failed to process #{r.Id}");
					
					r.Status = SellGoldRequestStatus.Failed;
					r.TimeCompleted = DateTime.UtcNow;
					_dbContext.SaveChanges();
				}
			}
		}
	}
}
