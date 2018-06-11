using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.Google.Impl;

namespace Goldmint.QueueService.Workers {

	public sealed class DebugWorker : BaseWorker {

		private IServiceProvider _services;
		private AppConfig _appConfig;
		// private ApplicationDbContext _dbContext;
		// private IEthereumReader _ethereumReader;
		// private IEthereumWriter _ethereumWriter;

		public DebugWorker() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_appConfig = services.GetRequiredService<AppConfig>();
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
