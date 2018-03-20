using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class SellingRequestProcessor : BaseWorker {

		private readonly int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		
		public SellingRequestProcessor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			_dbContext.DetachEverything();

			var nowTime = DateTime.UtcNow;

			var rows = await (
				from r in _dbContext.SellRequest
				where
				(r.Type == GoldExchangeRequestType.EthRequest || r.Type == GoldExchangeRequestType.HWRequest ) &&
				(r.Status == GoldExchangeRequestStatus.Prepared || r.Status == GoldExchangeRequestStatus.BlockchainConfirm) &&
				r.TimeNextCheck <= nowTime
				select new { Type = r.Type, Id = r.Id }
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {
				if (row.Type == GoldExchangeRequestType.HWRequest) {
					await CoreLogic.Finance.Tokens.GoldToken.ProcessHWSellingRequest(_services, row.Id);
				}
				if (row.Type == GoldExchangeRequestType.EthRequest) {
					await CoreLogic.Finance.Tokens.GoldToken.ProcessEthSellingRequest(_services, row.Id);
				}
			}
		}
	}
}
