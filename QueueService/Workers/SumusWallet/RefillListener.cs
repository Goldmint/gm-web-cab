using Goldmint.Common;
using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using System.IO;
using Goldmint.CoreLogic.Services.Bus.Nats;

namespace Goldmint.QueueService.Workers.SumusWallet {

	public class RefillListener: BaseWorker {

		private IServiceProvider _services;
		private ILogger _logger;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public RefillListener() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_logger = services.GetLoggerFor(this.GetType());
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			using (var sub = _natsConn.SubscribeSync(Sumus.Refiller.Refilled.Subject)) {
				while (!IsCancelled()) {
					try {
						var msg = sub.NextMessage(1000);
						try {
							_dbContext.DetachEverything();

							// read msg
							var req = Serializer.Deserialize<Sumus.Refiller.Refilled.Request>(msg.Data);

							// find wallet
							var row = await (
								from r in _dbContext.UserSumusWallet
								where r.PublicKey == req.Wallet
								select r
							)
							.AsNoTracking()
							.LastAsync();
							if (row == null) {
								throw new Exception($"Wallet #{req.Wallet} not found");
							}

							// parse amount
							var ok = decimal.TryParse(req.Amount, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var amount);
							if (!ok) {
								throw new Exception($"Failed to parse token");
							}
							// truncate
							amount = amount.ToSumus().FromSumus();

							// parse token
							ok = Goldmint.Common.Sumus.Token.ParseToken(req.Token, out var token);
							if (!ok) {
								throw new Exception($"Failed to parse token");
							}

							// refill
							if (!await CoreLogic.Finance.SumusWallet.Refill(_services, row.UserId, amount, token)) {
								throw new Exception($"Failed to process refilling");
							}

							// reply
							var rep = new Sumus.Refiller.Refilled.Reply() { Success = true };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
						catch (Exception e) {
							_logger.Error(e, $"Failed to process message");

							// reply
							var rep = new Sumus.Refiller.Refilled.Reply() { Success = false, Error = e.ToString() };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
					} catch{ }
				}
			}
		}
	}
}
