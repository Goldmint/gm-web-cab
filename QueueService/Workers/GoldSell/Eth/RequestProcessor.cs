using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.GoldSell.Eth {

	// RequestProcessor gets ready-to-send GOLD/ETH selling request and enqueues Ethereum transaction
	public sealed class RequestProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;
		private IEthereumWriter _ethereumWriter;

		public RequestProcessor(BaseOptions opts) : base(opts) {
			_rowsPerRound = 50;
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();
			_ethereumWriter = services.GetRequiredService<IEthereumWriter>();
			return Task.CompletedTask;
		}

		protected override Task OnCleanup() {
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			_dbContext.DetachEverything();
			
			var rows = await 
				(from r in _dbContext.SellGoldEth where r.Status == BuySellGoldRequestStatus.Confirmed select r)
				.Include(_ => _.RelFinHistory)
				.AsTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;
			if (IsCancelled() || rows.Length == 0) return;

			var ethAmount = await _ethereumReader.GetEtherBalance(await _ethereumWriter.GetEthSender());

			foreach (var r in rows) {
				if (IsCancelled() || ethAmount < r.EthAmount.ToEther()) {
					return;
				}

				try {
					using (var tx = await _dbContext.Database.BeginTransactionAsync()) {
						
						r.Status = BuySellGoldRequestStatus.Success;
						r.TimeCompleted = DateTime.UtcNow;
						_dbContext.SaveChanges();

						var sending = new DAL.Models.EthSending() {
							Status = EthereumOperationStatus.Initial,
							Amount = r.EthAmount,
							Address = r.Destination,
							TimeCreated = DateTime.UtcNow,
							TimeNextCheck = DateTime.UtcNow,
							RelFinHistoryId = r.RelFinHistory?.Id,
							UserId = r.UserId,
						};
						_dbContext.EthSending.Add(sending);
						_dbContext.SaveChanges();

						tx.Commit();
					}
					ethAmount -= r.EthAmount.ToEther();

				} catch (Exception e) {
					Logger.Error(e, $"Failed to process #{r.Id}");
					
					try {
						r.Status = BuySellGoldRequestStatus.Failed;
						r.TimeCompleted = DateTime.UtcNow;
						if (r.RelFinHistory != null) r.RelFinHistory.Status = UserFinHistoryStatus.Failed;
						_dbContext.SaveChanges();

						await CoreLogic.Finance.SumusWallet.Refill(_services, r.UserId, r.GoldAmount, SumusToken.Gold);
					} catch (Exception e1) {
						Logger.Error(e1, $"Failed to update request status #{r.Id}");
					}
				}
			}
		}
	}
}
