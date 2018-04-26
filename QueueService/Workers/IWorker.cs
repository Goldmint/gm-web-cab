using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public interface IWorker {

		Task Loop(CancellationToken ct);
		Task Init(IServiceProvider services);
	}
}
