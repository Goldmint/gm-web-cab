using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Bus.Telemetry;

namespace Goldmint.QueueService.Workers.Ethereum {
	
	//public sealed class EthereumOprationsProcessor : BaseWorker {

	//	private readonly int _rowsPerRound;
	//	private readonly int _ethConfirmations;

	//	private IServiceProvider _services;
	//	private ApplicationDbContext _dbContext;
	//	private CoreTelemetryAccumulator _coreTelemetryAccum;

	//	private long _statProcessed = 0;
	//	private long _statFailed = 0;

	//	public EthereumOprationsProcessor(int rowsPerRound, int ethConfirmations) {
	//		_rowsPerRound = Math.Max(1, rowsPerRound);
	//		_ethConfirmations = Math.Max(2, ethConfirmations);
	//	}

	//	protected override Task OnInit(IServiceProvider services) {
	//		_services = services;
	//		_dbContext = services.GetRequiredService<ApplicationDbContext>();
	//		_coreTelemetryAccum = services.GetRequiredService<CoreTelemetryAccumulator>();

	//		return Task.CompletedTask;
	//	}

	//	protected override async Task OnUpdate() {

	//		_dbContext.DetachEverything();

	//		var nowTime = DateTime.UtcNow;

	//		var rows = await (
	//			from r in _dbContext.EthereumOperation
	//			where 
	//			(r.Status == EthereumOperationStatus.Prepared || r.Status == EthereumOperationStatus.BlockchainConfirm) &&
	//			r.TimeNextCheck <= nowTime
	//			select new { Id = r.Id }
	//		)
	//			.AsNoTracking()
	//			.Take(_rowsPerRound)
	//			.ToArrayAsync(CancellationToken)
	//		;

	//		if (IsCancelled()) return;

	//		foreach (var row in rows) {

	//			if (IsCancelled()) return;

	//			_dbContext.DetachEverything();

	//			if (await CoreLogic.Finance.EthereumContract.ExecuteOperation(_services, row.Id, _ethConfirmations)) {
	//				++_statProcessed;
	//			}
	//			else {
	//				++_statFailed;
	//			}
	//		}
	//	}

	//	protected override void OnPostUpdate() {

	//		// tele
	//		_coreTelemetryAccum.AccessData(tel => {
	//			tel.EthereumOperations.Load = StatAverageLoad;
	//			tel.EthereumOperations.Exceptions = StatExceptionsCounter;
	//			tel.EthereumOperations.ProcessedSinceStartup = _statProcessed;
	//			tel.EthereumOperations.FailedSinceStartup = _statFailed;
	//		});
	//	}
	//}
}
