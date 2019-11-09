using Goldmint.CoreLogic.Services.Bus;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	// NotificationSender receives request (from front-end service) to send notification defined by id (in DB)
	public class NotificationSender : BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IEmailSender _emailSender;
		private IBus _bus;
		private IConnection _conn;
		private Notification[] _pendingList;

		public NotificationSender(BaseOptions opts) : base(opts) {
		}

		protected override async Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = _services.GetRequiredService<ApplicationDbContext>();
			_emailSender = services.GetRequiredService<IEmailSender>();
			_bus = services.GetRequiredService<IBus>();
			_conn = await _bus.AllocateConnection();

			// pending notifications from db
			{
				_pendingList = await (
					from n in _dbContext.Notification
					where n.TimeToSend <= DateTime.UtcNow
					select n
				)
					.AsNoTracking()
					.ToArrayAsync()
				;
			}
		}

		protected override Task OnCleanup() {
			_conn.Close();
			_conn.Dispose();
			return Task.CompletedTask;
		}

		protected void HandleMessage(object _, MsgHandlerEventArgs args) {
			try {
				var request = CoreLogic.Services.Bus.Models.Core.Sub.NotificationSendRequest.Parser.ParseFrom(args.Message.Data);

				var row = 
					(from r in _dbContext.Notification where r.Id == (long)request.ID select r)
					.AsNoTracking()
					.LastOrDefault()
				;
				if (row == null) {
					throw new Exception($"Notification #{request.ID} not found");
				}

				if (ProcessNotification(row).Result) {
					_dbContext.Remove(row);
					_dbContext.SaveChanges();
				}

				// reply
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, 
					new CoreLogic.Services.Bus.Models.Core.Sub.NotificationSendResponse() { Success = true }.ToByteArray()
				);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to process message");

				// reply
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, 
					new CoreLogic.Services.Bus.Models.Core.Sub.NotificationSendResponse() { Success = false, Error = e.ToString() }.ToByteArray()
				);
			}
		}

		protected override Task OnUpdate() {
			
			// process pending (from DB)
			var t1 = Task.Run(async() => {
				using (var sc = _services.CreateScope()) {
					var dbContext = sc.ServiceProvider.GetRequiredService<ApplicationDbContext>();
					foreach (var row in _pendingList) {
						if (IsCancelled()) break;
						try {
							if (await ProcessNotification(row)) {
								dbContext.Remove(row);
								await dbContext.SaveChangesAsync();
							}
						} catch (Exception e) {
							Logger?.Error(e, $"Failed to process pending notification #{row.Id}");
						}
					}
				}
			});

			// nats
			var t2 = Task.Run(async() => {
				using (var sub = _conn.SubscribeAsync(
					CoreLogic.Services.Bus.Models.Core.Sub.Subjects.NotificationSendRequest,
					new EventHandler<MsgHandlerEventArgs>(HandleMessage))
				) {
					sub.Start();
					try { await Task.Delay(-1, base.CancellationToken); } catch (TaskCanceledException) { }
					await sub.DrainAsync();
				}
			});
			
			Task.WaitAll(t1, t2);
			return Task.CompletedTask;
		}

		private async Task<bool> ProcessNotification(Notification row) {
			switch (row.Type) {
				case Common.NotificationType.Email:
					var email = new EmailNotification();
					email.DeserializeContentFromString(row.JsonData);
					return await _emailSender.Send(email as EmailNotification);
			}
			return false;
		}
	}
}
