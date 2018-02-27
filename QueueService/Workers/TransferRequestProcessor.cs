﻿using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;

namespace Goldmint.QueueService.Workers {

	public class TransferRequestProcessor : BaseWorker {

		private int _rowsPerRound;

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		
		public TransferRequestProcessor(int rowsPerRound) {
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
				from r in _dbContext.TransferRequest
				where 
				(r.Status == ExchangeRequestStatus.Processing || r.Status == ExchangeRequestStatus.BlockchainConfirm) &&
				r.TimeNextCheck <= nowTime
				select new { Id = r.Id }
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync(CancellationToken)
			;

			if (IsCancelled()) return;

			foreach (var row in rows) {
				await CoreLogic.Finance.Tokens.GoldToken.ProcessHWTransferRequest(_services, row.Id);
			}
		}
	}
}