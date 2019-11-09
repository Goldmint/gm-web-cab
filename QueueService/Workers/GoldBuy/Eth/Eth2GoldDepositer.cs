using Goldmint.Common;
using Goldmint.Common.Extensions;
using Goldmint.CoreLogic.Services.Bus;
using Goldmint.CoreLogic.Services.Price;
using Goldmint.DAL;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers.GoldBuy.Eth {

	// Eth2GoldDepositer receives events from eth2gold service, completing GOLD buying request
	public sealed class Eth2GoldDepositer : BaseWorker {

		private IServiceProvider _services;
		private ApplicationDbContext _dbContext;
		private IConnection _conn;
		private IPriceSource _priceSource;

		public Eth2GoldDepositer(BaseOptions opts) : base(opts) {
		}

		protected override async Task OnInit(IServiceProvider services) {
			_services = services;
			_dbContext = services.GetRequiredService<ApplicationDbContext>();
			_conn = await services.GetRequiredService<IBus>().AllocateConnection();
			_priceSource = services.GetRequiredService<IPriceSource>();
		}

		protected override Task OnCleanup() {
			_conn.Close();
			_conn.Dispose();
			return Task.CompletedTask;
		}

		protected override async Task OnUpdate() {
			using (var sub = _conn.SubscribeAsync(
				Eth2Gold.Subject.Event.OrderDeposited, 
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

				var evt = Eth2Gold.Event.OrderDeposited.Parser.ParseFrom(args.Message.Data);

				// find request
				var id = (long)evt.ExternalID;
				var row = 
					(from r in _dbContext.BuyGoldEth where r.Id == id select r)
					.AsTracking()
					.LastOrDefault()
				;

				BigInteger ethAmount = BigInteger.Zero;
				try {
					if (row == null) {
						throw new Exception("Order not found by given external id");
					}
					if (!BigInteger.TryParse(evt.EthAmount, out ethAmount) || ethAmount <= 0) {
						throw new Exception("Failed to parse ETH amount");
					}
				} catch (Exception e) {
					Logger.Error(e, $"Failed to process order #{id}");
					return;
				}

				if (row.Status == BuySellGoldRequestStatus.Confirmed) {

					long? dealGoldPrice = row.GoldRateCents;
					long? dealEthPrice = row.EthRateCents;

					// use fresh prices (see BuyGoldCrypto())
					if ((DateTime.UtcNow - row.TimeCreated) >= TimeSpan.FromHours(1)) {
						dealGoldPrice = null;
						dealEthPrice = null;
					}
					
					var estimation = CoreLogic.Finance.Estimation.BuyGoldCrypto(
						services: _services,
						ethereumToken: TradableCurrency.Eth,
						fiatCurrency: row.ExchangeCurrency,
						cryptoAmount: ethAmount,
						knownGoldRateCents: dealGoldPrice,
						knownCryptoRateCents: dealEthPrice
					).Result;

					// price provider failed
					if (!estimation.Allowed) {
						Logger.Error($"Failed to process order #{id}, estimation failed");
						return;
					}

					try {
						// history
						var finHistory = new DAL.Models.UserFinHistory() {
							Status = UserFinHistoryStatus.Completed,
							Type = UserFinHistoryType.GoldDeposit,
							Source = "ETH",
							SourceAmount = TextFormatter.FormatTokenAmountFixed(ethAmount, TokensPrecision.Ethereum),
							Destination = "GOLD",
							DestinationAmount = TextFormatter.FormatTokenAmountFixed(estimation.ResultGoldAmount, TokensPrecision.Sumus),
							Comment = "",
							TimeCreated = DateTime.UtcNow,
							UserId = row.UserId,
						};
						_dbContext.UserFinHistory.Add(finHistory);
						_dbContext.SaveChanges();

						row.Status = BuySellGoldRequestStatus.Success;
						row.TimeCompleted = DateTime.UtcNow;
						row.RelFinHistoryId = finHistory.Id;
						_dbContext.SaveChanges();

						var ok = CoreLogic.Finance.SumusWallet.Refill(_services, row.UserId, estimation.ResultGoldAmount.FromSumus(), SumusToken.Gold).Result;
						if (!ok) {
							Logger.Error($"Deposit request #{row.Id} completed but wallet not refilled with {estimation.ResultGoldAmount.FromSumus()} GOLD");
						} else {
							Logger.Information($"Deposit request #{row.Id} completed");
						}
					} catch (Exception e) {
						Logger.Error(e, $"Failed to process order #{id}, while saving to DB");
						return;
					}
				}

				// reply
				args.Message.ArrivalSubcription.Connection.Publish(
					args.Message.Reply, new Eth2Gold.Event.OrderDepositedAck().ToByteArray()
				);
			}
			catch (Exception e) {
				Logger.Error(e, $"Failed to process message");
			}
		}
	}
}
