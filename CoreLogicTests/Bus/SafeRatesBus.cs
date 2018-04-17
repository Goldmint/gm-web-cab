using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using Goldmint.CoreLogic.Services.Rate.Impl;
using System;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Bus {

	public sealed class SafeRatesBus : Test {

		private readonly Uri _pubBind = new Uri("tcp://localhost:30666");

		public SafeRatesBus(ITestOutputHelper testOutput) : base(testOutput) {
		}

		[Fact]
		public void PubSubSync() {
			using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {

					pub.PublishMessage(new SafeRatesMessage() {
						Rates = new[] {
							new SafeRate() {
								Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
								Stamp = 1, Ttl = 2, Usd = 3,
							},
							new SafeRate() {
								Currency = CurrencyRateType.Eth, CanBuy = true, CanSell = true,
								Stamp = 4, Ttl = 5, Usd = 6,
							}
						}
					});
					pub.PublishMessage(new SafeRatesMessage() {
						Rates = new[] {
							new SafeRate() {
								Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
								Stamp = 0xFFFFFFFFFF, Ttl = 0x8FFFFFFF, Usd = 0xDEADBEEF,
							},
						}
					});
					pub.PublishMessage(new SafeRatesMessage());

					SafeRatesMessage ratesMessage = null;

					Assert.True(sub.ReceiveBlocking(out ratesMessage));
					Assert.True(ratesMessage.Rates[0].Currency == CurrencyRateType.Gold && ratesMessage.Rates[0].CanBuy && ratesMessage.Rates[0].CanSell && ratesMessage.Rates[0].Stamp == 1 && ratesMessage.Rates[0].Ttl == 2 && ratesMessage.Rates[0].Usd == 3);
					Assert.True(ratesMessage.Rates[1].Currency == CurrencyRateType.Eth && ratesMessage.Rates[1].CanBuy && ratesMessage.Rates[1].CanSell && ratesMessage.Rates[1].Stamp == 4 && ratesMessage.Rates[1].Ttl == 5 && ratesMessage.Rates[1].Usd == 6);
					Assert.True(ratesMessage.Rates.Length == 2);

					Assert.True(sub.ReceiveBlocking(out ratesMessage));
					Assert.True(ratesMessage.Rates[0].Currency == CurrencyRateType.Gold && ratesMessage.Rates[0].CanBuy && ratesMessage.Rates[0].CanSell && ratesMessage.Rates[0].Stamp == 0xFFFFFFFFFF && ratesMessage.Rates[0].Ttl == 0x8FFFFFFF && ratesMessage.Rates[0].Usd == 0xDEADBEEF);
					Assert.True(ratesMessage.Rates.Length == 1);

					Assert.True(sub.ReceiveBlocking(out ratesMessage));
					Assert.True(ratesMessage.Rates.Length == 0);
				}
			}

			Thread.Sleep(1000);
		}

		[Fact]
		public void PubSubAsyncIntensive() {

			var stopCts = new CancellationTokenSource();
			var stopToken = stopCts.Token;
			var received = 0;
			var messages = 1000;

			// pub part
			var tp = Task.Factory.StartNew(() => {
				using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
					for (var i = 0; i < messages; ++i) {
						switch (SecureRandom.GetPositiveInt() % 2) {
							case 0: pub.PublishMessage(new SafeRatesMessage());
								break;
							case 1: pub.PublishMessage(new SafeRatesMessage() { Rates = new SafeRate[] { new SafeRate() { Currency = CurrencyRateType.Gold}, } });
								break;
						}
					}
				}
			});

			// sub part
			var ts = Task.Factory.StartNew(() => {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
					sub.SetCallback((subscriber, message) => {
						received++;
						if (received >= messages) {
							Assert.True(true, $"Got { received } messages");
							subscriber.Stop();
						}}
					);
					sub.Run();

					while (sub.IsRunning()) {
						Thread.Sleep(1);
					}
				}
			});
			stopCts.CancelAfter(5000);

			Task.WaitAll(tp, ts);
			stopCts.Dispose();

			Thread.Sleep(1000);
		}

		[Fact]
		public void PubSubAsync() {
			using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {

					Task.Factory.StartNew(() => {

						Thread.Sleep(250);
						pub.PublishMessage(new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
									Stamp = 1, Ttl = 2, Usd = 3,
								},
								new SafeRate() {
									Currency = CurrencyRateType.Eth, CanBuy = true, CanSell = true,
									Stamp = 4, Ttl = 5, Usd = 6,
								}
							}
						});

						Thread.Sleep(250);
						pub.PublishMessage(new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
									Stamp = 0xFFFFFFFFFF, Ttl = 0x8FFFFFFF, Usd = 0xDEADBEEF,
								},
							}
						});

						Thread.Sleep(250);
						pub.PublishMessage(new SafeRatesMessage());

					});

					var r = 0;
					sub.SetCallback((self, ratesMessage) => {
						if (r == 0) {
							Assert.True(ratesMessage.Rates[0].Currency == CurrencyRateType.Gold && ratesMessage.Rates[0].CanBuy && ratesMessage.Rates[0].CanSell && ratesMessage.Rates[0].Stamp == 1 && ratesMessage.Rates[0].Ttl == 2 && ratesMessage.Rates[0].Usd == 3);
							Assert.True(ratesMessage.Rates[1].Currency == CurrencyRateType.Eth && ratesMessage.Rates[1].CanBuy && ratesMessage.Rates[1].CanSell && ratesMessage.Rates[1].Stamp == 4 && ratesMessage.Rates[1].Ttl == 5 && ratesMessage.Rates[1].Usd == 6);
							Assert.True(ratesMessage.Rates.Length == 2);
						}
						else if (r == 1) {
							Assert.True(ratesMessage.Rates[0].Currency == CurrencyRateType.Gold && ratesMessage.Rates[0].CanBuy && ratesMessage.Rates[0].CanSell && ratesMessage.Rates[0].Stamp == 0xFFFFFFFFFF && ratesMessage.Rates[0].Ttl == 0x8FFFFFFF && ratesMessage.Rates[0].Usd == 0xDEADBEEF);
							Assert.True(ratesMessage.Rates.Length == 1);
						}
						else if (r == 2) {
							Assert.True(ratesMessage.Rates.Length == 0);
							self.Stop();
						}
						++r;
					});
					sub.Run();

					using (var cts = new CancellationTokenSource(1000)) {
						while (sub.IsRunning() && !cts.IsCancellationRequested) {
							Thread.Sleep(50);
						}
					}
				}
			}

			Thread.Sleep(1000);
		}

		[Fact]
		public void DefaultUnsafeRates() {

			// subscriber source
			using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				using (var source = new BusSafeRatesSource(sub, LogFactory)) {

					var gold = source.GetRate(CurrencyRateType.Gold);
					Assert.True(gold != null);
					Assert.True(gold.Usd == 0);
					Assert.False(gold.IsSafeForBuy);
					Assert.False(gold.IsSafeForSell);

					var crypto = source.GetRate(CurrencyRateType.Eth);
					Assert.True(crypto != null);
					Assert.True(crypto.Usd == 0);
					Assert.False(crypto.IsSafeForBuy);
					Assert.False(crypto.IsSafeForSell);
				}
			}

			// dispatcher source
			using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);
				using (var source = new SafeRatesDispatcher(pubWrapper, LogFactory)) {

					var gold = source.GetRate(CurrencyRateType.Gold);
					Assert.True(gold != null);
					Assert.True(gold.Usd == 0);
					Assert.False(gold.IsSafeForBuy);
					Assert.False(gold.IsSafeForSell);

					var crypto = source.GetRate(CurrencyRateType.Eth);
					Assert.True(crypto != null);
					Assert.True(crypto.Usd == 0);
					Assert.False(crypto.IsSafeForBuy);
					Assert.False(crypto.IsSafeForSell);
				}
			}

			Thread.Sleep(1000);
		}

		[Fact]
		public void FreshAndStaleRates() {

			using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
					using (var source = new BusSafeRatesSource(sub, LogFactory)) {

						var ratesMessage = (SafeRatesMessage)null;
						var stamp = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
						var freshFor = 2;

						// send fresh
						pub.PublishMessage(new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
									Stamp = stamp, Ttl = freshFor, Usd = 0xDEADBEEF,
								},
								new SafeRate() {
									Currency = CurrencyRateType.Eth, CanBuy = true, CanSell = true,
									Stamp = stamp, Ttl = freshFor, Usd = 0xDEADBEEE,
								}
							}
						});
						Assert.True(sub.ReceiveBlocking(out ratesMessage));
						source.OnNewRates(sub, ratesMessage);

						// got fresh
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF);
						Assert.True(source.GetRate(CurrencyRateType.Gold).IsSafeForBuy && source.GetRate(CurrencyRateType.Gold).IsSafeForSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE);
						Assert.True(source.GetRate(CurrencyRateType.Eth).IsSafeForBuy && source.GetRate(CurrencyRateType.Eth).IsSafeForSell);

						// timeout
						Thread.Sleep(freshFor * 1000 + 100);

						// become stale/unsafe
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF);
						Assert.True(!source.GetRate(CurrencyRateType.Gold).IsSafeForBuy && !source.GetRate(CurrencyRateType.Gold).IsSafeForSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE);
						Assert.True(!source.GetRate(CurrencyRateType.Eth).IsSafeForBuy && !source.GetRate(CurrencyRateType.Eth).IsSafeForSell);

						// send stale
						pub.PublishMessage(new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
									Stamp = stamp + 1, Ttl = 0, Usd = 0xDEADBEEF + 0xD,
								},
								new SafeRate() {
									Currency = CurrencyRateType.Eth, CanBuy = true, CanSell = true,
									Stamp = stamp + 1, Ttl = 0, Usd = 0xDEADBEEE + 0xD,
								}
							}
						});
						Assert.True(sub.ReceiveBlocking(out ratesMessage));
						source.OnNewRates(sub, ratesMessage);

						// got stale
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF + 0xD);
						Assert.True(!source.GetRate(CurrencyRateType.Gold).IsSafeForBuy && !source.GetRate(CurrencyRateType.Gold).IsSafeForSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE + 0xD);
						Assert.True(!source.GetRate(CurrencyRateType.Eth).IsSafeForBuy && !source.GetRate(CurrencyRateType.Eth).IsSafeForSell);

						// send fresh again
						stamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
						pub.PublishMessage(new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = false, CanSell = true,
									Stamp = stamp, Ttl = freshFor, Usd = 0xDEADBEEF + 0xDE,
								},
								new SafeRate() {
									Currency = CurrencyRateType.Eth, CanBuy = true, CanSell = false,
									Stamp = stamp, Ttl = freshFor, Usd = 0xDEADBEEE + 0xDE,
								}
							}
						});
						Assert.True(sub.ReceiveBlocking(out ratesMessage));
						source.OnNewRates(sub, ratesMessage);

						// got fresh
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF + 0xDE);
						Assert.True(!source.GetRate(CurrencyRateType.Gold).IsSafeForBuy && source.GetRate(CurrencyRateType.Gold).IsSafeForSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE + 0xDE);
						Assert.True(source.GetRate(CurrencyRateType.Eth).IsSafeForBuy && !source.GetRate(CurrencyRateType.Eth).IsSafeForSell);
					}

				}
			}

			Thread.Sleep(1000);
		}

		[Fact]
		public void DispatcherRunStop() {

			var randomRates = new DebugRateProvider();

			using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
				var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);

				using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory)) { }

				using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory)) {
					dispatcher.Run(TimeSpan.FromSeconds(0.02));

					for (var k = 0; k < 100; k++) {
						for (var i = 0; i < 100; i++) {
							dispatcher.OnProviderCurrencyRate(randomRates.RequestGoldRate(TimeSpan.Zero).Result);
							dispatcher.OnProviderCurrencyRate(randomRates.RequestEthRate(TimeSpan.Zero).Result);
						}
						Thread.Sleep(15);
					}
				}
			}

			Thread.Sleep(1000);
		}

		[Fact]
		public void DispatcherRates() {

			var randomRates = new DebugRateProvider();
			var stopCts = new CancellationTokenSource();
			var stopToken = stopCts.Token;

			// pub part
			var tp = Task.Factory.StartNew(() => {
				using (var pub = new DefaultPublisher<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
					var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);
					using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory)) {

						dispatcher.Run(TimeSpan.FromSeconds(0.02));

						while (!stopToken.IsCancellationRequested) {
							dispatcher.OnProviderCurrencyRate(
								SecureRandom.GetPositiveInt() % 2 == 0
								? randomRates.RequestGoldRate(TimeSpan.Zero).Result
								: randomRates.RequestEthRate(TimeSpan.Zero).Result
							);
							Thread.Sleep(5);
						}
					}
				}
			});

			// sub part
			var ts = Task.Factory.StartNew(() => {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, _pubBind, LogFactory)) {
					sub.Run();
					using (var source = new BusSafeRatesSource(sub, LogFactory)) {

						var goldOk = false;
						var ethOk = false;
						while (!goldOk && !ethOk) {

							var goldRate = source.GetRate(CurrencyRateType.Gold);
							var ethRate = source.GetRate(CurrencyRateType.Eth);

							if (goldRate.IsSafeForBuy && goldRate.IsSafeForSell) {
								goldOk = true;
							}
							if (ethRate.IsSafeForBuy && ethRate.IsSafeForSell) {
								ethOk = true;
							}
							Thread.Sleep(10);
						}

						stopCts.Cancel();
					}
				}
			});
			
			Task.WaitAll(tp, ts);
			stopCts.Dispose();

			Thread.Sleep(1000);
		}
	}
}
