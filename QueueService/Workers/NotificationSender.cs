using Goldmint.CoreLogic.Services.Bus;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public sealed class NotificationSender : BaseWorker {

		private IServiceProvider _services;
		private IEmailSender _emailSender;
		private IConnPool _bus;
		private Notification[] _pendingList;

		public NotificationSender() {
		}

		protected override async Task OnInit(IServiceProvider services) {
			_services = services;
			_emailSender = services.GetRequiredService<IEmailSender>();
			_bus = services.GetRequiredService<IConnPool>();

			// pending notifications from db
			{
				var dbContext = _services.GetRequiredService<ApplicationDbContext>();
				_pendingList = await (
					from n in dbContext.Notification
					where n.TimeToSend <= DateTime.UtcNow
					select n
				)
					.AsNoTracking()
					.ToArrayAsync()
				;
			}
		}

		protected override Task OnUpdate() {
			
			// process pending
			var t1 = Task.Run(async() => {
				var dbContext = _services.GetRequiredService<ApplicationDbContext>();
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
			});

			// nats
			var t2 = Task.Run(async() => {
				var dbContext = _services.GetRequiredService<ApplicationDbContext>();
				using (var conn = await _bus.GetConnection()) {
					using (var sub = conn.SubscribeSync(CoreLogic.Services.Bus.Models.Notification.Enqueued.Subject)) {
						while (!IsCancelled()) {
							try {
								var msg = sub.NextMessage(1000);
								try {
									dbContext.DetachEverything();

									// request/reply
									var req = Serializer.Deserialize<CoreLogic.Services.Bus.Models.Notification.Enqueued.Request>(msg.Data);

									// process
									{
										var row = await (
											from r in dbContext.Notification
											where r.Id == req.Id
											select r
										)
										.AsNoTracking()
										.LastAsync();
										if (row == null) {
											throw new Exception($"Notification #{req.Id} not found");
										}
										if (await ProcessNotification(row)) {
											dbContext.Remove(row);
											await dbContext.SaveChangesAsync();
										}
									}

									// reply
									conn.Publish(
										msg.Reply, 
										Serializer.Serialize(
											new CoreLogic.Services.Bus.Models.Notification.Enqueued.Reply() { Success = true }
										)
									);
								}
								catch (Exception e) {
									Logger.Error(e, $"Failed to process message");

									// reply
									conn.Publish(
										msg.Reply, 
										Serializer.Serialize(
											new CoreLogic.Services.Bus.Models.Notification.Enqueued.Reply() { Success = false, Error = e.ToString() }
										)
									);
								}
							} catch{ }
						}
					}
					conn.Close();
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
