using Goldmint.CoreLogic.Finance.Fiat;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class NotificationSender : BaseWorker {

		private int _rowsPerRound;

		private ApplicationDbContext _dbContext;
		private IServiceProvider _services;
		private INotificationQueue _notificationQueue;
		private IMutexHolder _mutexHolder;
		private IEmailSender _emailSender;

		public NotificationSender(int rowsPerRound) {
			_rowsPerRound = Math.Max(1, rowsPerRound);
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_notificationQueue = services.GetRequiredService<INotificationQueue>();
			_mutexHolder = services.GetRequiredService<IMutexHolder>();
			_emailSender = services.GetRequiredService<IEmailSender>();

			return Task.CompletedTask;
		}

		protected override async Task Loop() {

			var rows = await (
				from n in _dbContext.Notification
				where n.TimeToSend <= DateTime.UtcNow
				select n
			)
				.AsNoTracking()
				.Take(_rowsPerRound)
				.ToArrayAsync()
			;

			foreach (var r in rows) {

				// acquire lock
				var mutexBuilder = new MutexBuilder(_mutexHolder)
					.Mutex(Common.MutexEntity.NotificationSend, r.Id)
				;
				await mutexBuilder.LockAsync(async (ok) => {
					if (ok) {

						// deserialize
						BaseNotification noti = null;
						try {
							switch (r.Type) {
								case Common.NotificationType.Email:
									noti = new EmailNotification();
									break;
							}
							noti.DeserializeContentFromString(r.JsonData);
						}
						catch (Exception e) {
							Logger?.Error(e, "Failed to deserialize notification");
						}

						// remove from db
						if (noti == null || await ProcessNotification(noti)) {
							_dbContext.Remove(r);
						}

						// save changes on
						await _dbContext.SaveChangesAsync();
					}
				});
			}
		}

		private async Task<bool> ProcessNotification(BaseNotification row) {

			try {

				if (row.Type == Common.NotificationType.Email) {
					return await _emailSender.Send(row as EmailNotification);
				}

			} catch (Exception e) {
				Logger?.Error(e, "Failed to process {0} notification", row.Type.ToString());
			}

			return true;
		}
	}
}
