using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Bus;
using Goldmint.DAL;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.SumusWallet {

	// RefillListener listens mint-sender service for a new users' GOLD deposits
	public class RefillListener: BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IConnection _conn;

		public RefillListener(BaseOptions opts) : base(opts) {
		}

		protected override async Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_conn = await services.GetRequiredService<IBus>().AllocateConnection();
		}

		protected override Task OnCleanup() {
			_conn.Close();
			_conn.Dispose();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			using (var sub = _conn.SubscribeAsync(
				MintSender.Subject.Watcher.Event.Refill, 
				new EventHandler<MsgHandlerEventArgs>(HandleMessage))
			) {
				sub.Start();
				try { await Task.Delay(-1, base.CancellationToken); } catch (TaskCanceledException) { }
				await sub.DrainAsync();
			}
		}

		private void HandleMessage(object _, MsgHandlerEventArgs args) {
			try {
				_dbContext.DetachEverything();

				var request = MintSender.Watcher.Event.Refill.Parser.ParseFrom(args.Message.Data);
				
				if (request.Service != "core_gold_deposit") {
					return;
				}

				// find wallet
				var row = 
					(from r in _dbContext.UserSumusWallet where r.PublicKey == request.PublicKey select r)
					.AsNoTracking()
					.LastOrDefault()
				;
				if (row == null) {
					throw new Exception($"Wallet #{request.PublicKey} not found");
				}

				// parse amount
				var ok = decimal.TryParse(request.Amount, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var amount);
				if (!ok) {
					throw new Exception($"Failed to parse amount");
				}
				// truncate
				amount = amount.ToSumus().FromSumus();

				// parse token
				ok = Common.Sumus.Token.ParseToken(request.Token, out var token);
				if (!ok) {
					throw new Exception($"Failed to parse token");
				}

				// history
				var finHistory = new DAL.Models.UserFinHistory() {
					Status = UserFinHistoryStatus.Completed,
					Type = UserFinHistoryType.GoldDeposit,
					Source = "",
					SourceAmount = "",
					Destination = "GOLD",
					DestinationAmount = TextFormatter.FormatTokenAmountFixed(amount),
					Comment = "",
					TimeCreated = DateTime.UtcNow,
					UserId = row.UserId,
				};
				_dbContext.UserFinHistory.Add(finHistory);
				_dbContext.SaveChanges();

				// refill
				if (!CoreLogic.Finance.SumusWallet.Refill(_services, row.UserId, amount, token).Result) {
					throw new Exception($"Failed to process refilling");
				}

				// reply
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, new MintSender.Watcher.Event.RefillAck() { Success = true }.ToByteArray()
				);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to process message");

				// reply
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, new MintSender.Watcher.Event.RefillAck() { Success = false, Error = e.ToString() }.ToByteArray()
				);
			}
		}
	}
}
