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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Goldmint.QueueService {

	public partial class Program {

		private static List<IRPCServer> _rpcServers = new List<IRPCServer>();

		/// <summary>
		/// DI services
		/// </summary>
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
			services.AddScoped<ITicketDesk, DBTicketDesk>();

			// acquirer
			services.AddScoped<ICardAcquirer>(fac => {
				return new The1stPayments(opts => {
					opts.MerchantGuid = _appConfig.Services.The1StPayments.MerchantGuid;
					opts.ProcessingPassword = _appConfig.Services.The1StPayments.ProcessingPassword;
					opts.Gateway = _appConfig.Services.The1StPayments.Gateway;
				}, _loggerFactory);
			});

			// notifications
			services.AddSingleton<INotificationQueue, DBNotificationQueue>();

			// blockchain reader
			services.AddSingleton<IEthereumReader, EthereumReader>();

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
				// var rpcSvc = new WorkerRpcService(_loggerFactory);
				// _defaultRpcServer = new JsonRPCServer<WorkerRpcService>(rpcSvc, _loggerFactory);
				// _defaultRpcServer.Start(Environment.GetEnvironmentVariable("ASPNETCORE_RPC"));
			}

			if (Mode.HasFlag(WorkingMode.Service)) {

				// blockchain writer
				services.AddSingleton<IEthereumWriter, EthereumWriter>();

				// rates (could be added in section above)
				if (services.Count(x => x.ServiceType == typeof(IGoldRateProvider)) == 0) {
					services.AddSingleton<IGoldRateProvider>(fac => new GoldRateRpcProvider(_appConfig.RpcServices.GoldRateUsdUrl, _loggerFactory));
				}
			}
		}

		/// <summary>
		/// Launch RPC servers
		/// </summary>
		private static void SetupRpc(IServiceProvider services) {

			// worker rpc
			if (Mode.HasFlag(WorkingMode.Worker)) {

				var rpcSvc = new WorkerRpcService(
					services.CreateScope().ServiceProvider, 
					_loggerFactory
				);
				var rpcSrv = new JsonRPCServer<WorkerRpcService>(rpcSvc, _loggerFactory);
				rpcSrv.Start(Environment.GetEnvironmentVariable("ASPNETCORE_RPC"));

				_rpcServers.Add(rpcSrv);
			}
		}
	}
}
