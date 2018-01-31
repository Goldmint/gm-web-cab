using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class DebugWorker : BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEthereumReader _ethereumReader;

		public DebugWorker() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_ethereumReader = services.GetRequiredService<IEthereumReader>();

			return Task.CompletedTask;
		}

		protected override Task Loop() {
			try {
			} catch (Exception e) {
				Logger.Error(e);
			}
			return Task.CompletedTask;
		}
	}
}
