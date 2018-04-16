using Goldmint.CoreLogic.Services.Bus.Publisher;
using System;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using Xunit;

namespace CoreLogicTests.Bus {

	public class SafeRatesBus {

		private readonly Uri _pubBind = new Uri("tcp://localhost:30666");
		private readonly TimeSpan _freshRatesTimeout = TimeSpan.FromSeconds(2);

		[Fact]
		public void PubSub() {
			using (var pub = new SafeRatesPublisher(_pubBind, Test.LogFactory)) {
				using (var sub = new SafeRatesSubscriber(_pubBind, Test.LogFactory)) {

					pub.PublishRates(new SafeRates() {
						Gold = new Gold() {
							Usd = 666,
							IsSafeForSell = true,
							IsSafeForBuy = true,
						},
						Crypto = new Crypto() {
							EthUsd = 777,
							IsSafeForSell = true,
							IsSafeForBuy = true,
						},
					});

					pub.PublishRates(new SafeRates() {
						Gold = new Gold() {
							Usd = 666,
							IsSafeForSell = true,
							IsSafeForBuy = true,
						},
						Crypto = null
					});

					pub.PublishRates(new SafeRates());

					var r = 0;
					sub.SetCallback((self, rates) => {
						if (r == 0) {
							Assert.True(rates.Gold.Usd == 666 && rates.Gold.IsSafeForSell && rates.Gold.IsSafeForBuy);
							Assert.True(rates.Crypto.EthUsd == 777 && rates.Crypto.IsSafeForSell && rates.Crypto.IsSafeForBuy);
						}
						else if (r == 1) {
							Assert.True(rates.Gold.Usd == 666 && rates.Gold.IsSafeForSell && rates.Gold.IsSafeForBuy);
							Assert.True(rates.Crypto == null);
						}
						else if (r == 2) {
							Assert.True(rates.Gold == null);
							Assert.True(rates.Crypto == null);
							self.Stop();
						}
						++r;
					});
					sub.Run();

					using (var cts = new CancellationTokenSource(5000)) {
						while (sub.IsRunning() && !cts.IsCancellationRequested) {
							Thread.Sleep(50);
						}
					}
				}
			}
		}

		[Fact]
		public void DefaultUnsafeRates() {

			using (var sub = new SafeRatesSubscriber(_pubBind, Test.LogFactory)) {
				using (var source = new Goldmint.CoreLogic.Services.Rate.Impl.BusSafeRatesSource(sub, _freshRatesTimeout, Test.LogFactory)) {

					var gold = source.GetGoldRate();
					Assert.True(gold != null);
					Assert.True(gold.Usd == 0);
					Assert.False(gold.IsSafeForBuy);
					Assert.False(gold.IsSafeForSell);
					
					var crypto = source.GetCryptoRate();
					Assert.True(crypto != null);
					Assert.True(crypto.EthUsd == 0);
					Assert.False(crypto.IsSafeForBuy);
					Assert.False(crypto.IsSafeForSell);
				}
			}
		}

		[Fact]
		public void StaleRates() {

			using (var pub = new SafeRatesPublisher(_pubBind, Test.LogFactory)) {
				using (var sub = new SafeRatesSubscriber(_pubBind, Test.LogFactory)) {
					sub.Run();
					using (var source = new Goldmint.CoreLogic.Services.Rate.Impl.BusSafeRatesSource(sub, _freshRatesTimeout, Test.LogFactory)) {

						// send fresh
						pub.PublishRates(new SafeRates() {
							Stamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
							Gold = new Gold() {
								Usd = 666,
								IsSafeForSell = true,
								IsSafeForBuy = true,
							},
							Crypto = new Crypto() {
								EthUsd = 777,
								IsSafeForSell = true,
								IsSafeForBuy = true,
							},
						});
						Thread.Sleep(250);

						// got fresh
						Assert.True(source.GetGoldRate().Usd == 666);
						Assert.True(source.GetGoldRate().IsSafeForBuy && source.GetGoldRate().IsSafeForSell);
						Assert.True(source.GetCryptoRate().EthUsd == 777);
						Assert.True(source.GetCryptoRate().IsSafeForBuy && source.GetCryptoRate().IsSafeForSell);

						// didnt got fresh => stale, unsafe
						Thread.Sleep(_freshRatesTimeout.Add(TimeSpan.FromSeconds(1)));

						// become unsafe
						Assert.True(source.GetGoldRate().Usd == 0);
						Assert.False(source.GetGoldRate().IsSafeForBuy && source.GetGoldRate().IsSafeForSell);
						Assert.True(source.GetCryptoRate().EthUsd == 0);
						Assert.False(source.GetCryptoRate().IsSafeForBuy && source.GetCryptoRate().IsSafeForSell);

						// send stale
						pub.PublishRates(new SafeRates() {
							Stamp = 0,
							Gold = new Gold() {
								Usd = 666,
								IsSafeForSell = true,
								IsSafeForBuy = true,
							},
							Crypto = new Crypto() {
								EthUsd = 777,
								IsSafeForSell = true,
								IsSafeForBuy = true,
							},
						});
						Thread.Sleep(250);

						// got stale
						Assert.True(source.GetGoldRate().Usd == 0);
						Assert.False(source.GetGoldRate().IsSafeForBuy && source.GetGoldRate().IsSafeForSell);
						Assert.True(source.GetCryptoRate().EthUsd == 0);
						Assert.False(source.GetCryptoRate().IsSafeForBuy && source.GetCryptoRate().IsSafeForSell);

						// send fresh again (safe only for buy)
						pub.PublishRates(new SafeRates() {
							Stamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
							Gold = new Gold() {
								Usd = 1,
								IsSafeForSell = false,
								IsSafeForBuy = true,
							},
							Crypto = new Crypto() {
								EthUsd = 1,
								IsSafeForSell = false,
								IsSafeForBuy = true,
							},
						});
						Thread.Sleep(250);

						// got fresh
						Assert.True(source.GetGoldRate().Usd > 0);
						Assert.True(source.GetGoldRate().IsSafeForBuy && !source.GetGoldRate().IsSafeForSell);
						Assert.True(source.GetCryptoRate().EthUsd > 0);
						Assert.True(source.GetCryptoRate().IsSafeForBuy && !source.GetCryptoRate().IsSafeForSell);
					}

				}
			}
		}
	}
}
