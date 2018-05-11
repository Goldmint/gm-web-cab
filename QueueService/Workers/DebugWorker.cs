using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public sealed class DebugWorker : BaseWorker {

		private IServiceProvider _services;
		// private ApplicationDbContext _dbContext;
		// private IEthereumReader _ethereumReader;
		// private IEthereumWriter _ethereumWriter;

		public DebugWorker() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			// _dbContext = services.GetRequiredService<ApplicationDbContext>();
			// _ethereumReader = services.GetRequiredService<IEthereumReader>();
			// _ethereumWriter = services.GetRequiredService<IEthereumWriter>();

			return Task.CompletedTask;
		}

		protected override Task OnUpdate() {
			try {
			} catch (Exception e) {
				Logger.Error(e);
			}
			return Task.CompletedTask;
		}
	}
}
