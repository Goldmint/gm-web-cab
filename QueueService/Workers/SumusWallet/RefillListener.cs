using Goldmint.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Bus.Nats;

namespace Goldmint.QueueService.Workers.SumusWallet {

	public class RefillListener: BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private NATS.Client.IConnection _natsConn;

		public RefillListener() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_natsConn = services.GetRequiredService<NATS.Client.IConnection>();
			return Task.CompletedTask;
		}

		protected override void OnCleanup() {
			_natsConn.Close();
		}

		protected override async Task OnUpdate() {
			using (var sub = _natsConn.SubscribeSync(MintSender.Watcher.Refill.Subject)) {
				while (!IsCancelled()) {
					try {
						var msg = sub.NextMessage(1000);
						try {
							_dbContext.DetachEverything();

							// read msg
							var req = Serializer.Deserialize<MintSender.Watcher.Refill.Request>(msg.Data);

							if (req.Service != MintSender.CoreService) {
								continue;
							}

							// find wallet
							var row = await (
								from r in _dbContext.UserSumusWallet
								where r.PublicKey == req.PublicKey
								select r
							)
							.AsNoTracking()
							.LastAsync()
							;
							if (row == null) {
								throw new Exception($"Wallet #{req.PublicKey} not found");
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
							var rep = new MintSender.Watcher.Refill.Reply() { Success = true };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
						catch (Exception e) {
							Logger.Error(e, $"Failed to process message");

							// reply
							var rep = new MintSender.Watcher.Refill.Reply() { Success = false, Error = e.ToString() };
							_natsConn.Publish(msg.Reply, Serializer.Serialize(rep));
						}
					} catch{ }
				}
			}
		}
	}
}
