using Goldmint.CoreLogic.Services.Acquiring;
using Goldmint.CoreLogic.Services.Acquiring.Impl;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Blockchain.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.RPC;
using Goldmint.CoreLogic.Services.RPC.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.CoreLogic.Services.Ticket.Impl;
using Goldmint.DAL;
using Goldmint.QueueService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Goldmint.QueueService {

	public partial class Program {

		private static IRPCServer _defaultRpcServer;

		private static void SetupCommonServices(ServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(_loggerFactory);

			// db
			services.AddDbContext<ApplicationDbContext>(opts => {
				opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
					myopts.UseRelationalNulls(true);
				});
			});

			// mutex
			services.AddScoped<IMutexHolder, DBMutexHolder>(); 

			// templates
			services.AddSingleton<ITemplateProvider, TemplateProvider>();

			// ticket desk
			services.AddSingleton<ITicketDesk, DBTicketDesk>();

			// acquirer
			services.AddScoped<ICardAcquirer>(fac => {
				return new The1stPayments(opts => {
					opts.MerchantGuid = _appConfig.Services.The1stPayments.MerchantGuid;
					opts.ProcessingPassword = _appConfig.Services.The1stPayments.ProcessingPassword;
					opts.Gateway = _appConfig.Services.The1stPayments.Gateway;
				}, _loggerFactory);
			});

			// notifications
			services.AddSingleton<INotificationQueue, DBNotificationQueue>();

			// blockchain reader
			services.AddSingleton<IEthereumReader, InfuraReader>();

			// ---

			if (Mode.HasFlag(WorkingMode.Worker)) {

				// mail sender
				if (!_environment.IsDevelopment()) {
					services.AddSingleton<IEmailSender, MailGunSender>();
				}
				else {
					services.AddSingleton<IEmailSender, NullEmailSender>();
				}

				// rates
				services.AddSingleton<IGoldRateProvider>(new LocalGoldRateProvider());

				// rpc server
				var workerRPCService = new WorkerRPCService();
				_defaultRpcServer = new JsonRPCServer<WorkerRPCService>(workerRPCService, _loggerFactory);
				_defaultRpcServer.Start(Environment.GetEnvironmentVariable("ASPNETCORE_RPC"));
			}

			if (Mode.HasFlag(WorkingMode.Service)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, InfuraWriter>();

				// rates (could be added in section above)
				if (services.Count(x => x.ServiceType == typeof(IGoldRateProvider)) == 0) {
					services.AddSingleton<IGoldRateProvider>(fac => new GoldRateRpcProvider(_appConfig.RpcServices.GoldRateUsdUrl, _loggerFactory));
				}
			}
		}
	}
}
