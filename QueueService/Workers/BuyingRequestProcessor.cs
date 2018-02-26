using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;

namespace Goldmint.QueueService.Workers {

	public class BuyingRequestProcessor : BaseWorker {

		private int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		
		public BuyingRequestProcessor(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			var nowTime = DateTime.UtcNow;

			var rows = await (
				from r in _dbContext.BuyRequest
				where 
				(r.Type == ExchangeRequestType.EthRequest || r.Type == ExchangeRequestType.HWRequest ) &&
				(r.Status == ExchangeRequestStatus.Processing || r.Status == ExchangeRequestStatus.BlockchainConfirm) &&
				r.TimeNextCheck <= nowTime
				select new { Type = r.Type, Id = r.Id }
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {
				if (row.Type == ExchangeRequestType.HWRequest) {
					await CoreLogic.Finance.Tokens.GoldToken.ProcessHWBuyingRequest(_services, row.Id);
				}
				if (row.Type == ExchangeRequestType.EthRequest) {
					await CoreLogic.Finance.Tokens.GoldToken.ProcessEthBuyingRequest(_services, row.Id);
				}
			}
		}
	}
}
