using Goldmint.CoreLogic.Services.Bus.Proto;
using Goldmint.CoreLogic.Services.Bus.Publisher;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
using Goldmint.CoreLogic.Services.Rate.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Goldmint.Common;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Bus {

	public sealed class SafeRatesBus : Test {

		private int _portCounter = 45900;
		private Uri GetNextSocketAddress => new Uri($"tcp://localhost:{ (++_portCounter) }");

		public SafeRatesBus(ITestOutputHelper testOutput) : base(testOutput) {
		}

		protected override void DisposeManaged() {
			//NetMQ.NetMQConfig.Cleanup(false);
			base.DisposeManaged();
		}

		// ---

		[Fact]
		public void PubSubRunStop() {

			var socket = GetNextSocketAddress;

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
			}

			using (var sub = new DefaultSubscriber<SafeRatesMessage>(new[] {Topic.FiatRates}, socket, LogFactory)) {
			}

			socket = GetNextSocketAddress;

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();
				Thread.Sleep(500);
			}

			using (var sub = new DefaultSubscriber<SafeRatesMessage>(new[] {Topic.FiatRates}, socket, LogFactory)) {
				sub.Run();
				Thread.Sleep(500);
			}

			socket = GetNextSocketAddress;

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();
				Thread.Sleep(500);
				pub.StopAsync();
				while (pub.IsRunning()) {
					Thread.Sleep(50);
				}
			}

			using (var sub = new DefaultSubscriber<SafeRatesMessage>(new[] {Topic.FiatRates}, socket, LogFactory)) {
				sub.Run();
				Thread.Sleep(500);
				sub.StopAsync();
				while (sub.IsRunning()) {
					Thread.Sleep(50);
				}
			}
		}

		[Fact]
		public void PubSubAsync() {

			var socket = GetNextSocketAddress;

			var evSubReady = new ManualResetEventSlim();

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();

				using (var sub = new DefaultSubscriber<SafeRatesMessage>(new[] { Topic.FiatRates }, socket, LogFactory)) {

					Task.Factory.StartNew(() => {

						evSubReady.Wait();
						evSubReady.Reset();

						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() {
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

						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() {
							Rates = new[] {
								new SafeRate() {
									Currency = CurrencyRateType.Gold, CanBuy = true, CanSell = true,
									Stamp = 0xFFFFFFFFFF, Ttl = 0x8FFFFFFF, Usd = 0xDEADBEEF,
								},
							}
						});

						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage());
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
							self.StopAsync();
						}
						++r;
					});
					sub.Run();
					evSubReady.Set();

					while (sub.IsRunning()) {
						Thread.Sleep(50);
					}

					Assert.True(r == 3);
				}
			}
		}

		[Fact]
		public void PubSubAsyncIntensive() {

			var socket = GetNextSocketAddress;
			var messages = 10000;

			var evSubReady = new ManualResetEventSlim();

			// pub part
			var tp = Task.Factory.StartNew(() => {
				using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
					pub.Run();

					Assert.True(evSubReady.Wait(1000));
					evSubReady.Reset();

					for (var i = 0; i < messages; ++i) {
						switch (SecureRandom.GetPositiveInt() % 2) {
							case 0: pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage());
								break;
							case 1: pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() { Rates = new SafeRate[] { new SafeRate() { Currency = CurrencyRateType.Gold}, } });
								break;
						}
					}

					Assert.True(evSubReady.Wait(5000));
				}
			});

			// sub part
			var received = 0;
			var ts = Task.Factory.StartNew(() => {
				using (var sub = new DefaultSubscriber<SafeRatesMessage>(new[] { Topic.FiatRates }, socket, LogFactory)) {
					sub.SetCallback((subscriber, message) => {
						if (++received >= messages) {
							subscriber.StopAsync();
						}
					});
					sub.Run();
					evSubReady.Set();

					while (sub.IsRunning()) {
						Thread.Sleep(50);
					}

					evSubReady.Set();
				}
			});

			Task.WaitAll(tp, ts);

			Logger.Trace($"{messages} == {received}");
			Assert.True(messages == received);
		}

		[Fact]
		public void DispatcherRunStop() {

			var socket = GetNextSocketAddress;
			var randomRates = new DebugRateProvider();

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();

				var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);

				using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory, null)) { }

				using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory, null)) {
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
		}

		/*[Fact]
		public void SubReconnection1() {

			var socket = GetNextSocketAddress;
			var testStop = new ManualResetEventSlim();

			// pub part
			var tp = Task.Factory.StartNew(() => {

				for (var usd = 0; usd < 2; ++usd) {
					using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
						pub.Run();

						var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);
						using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory, null)) {

							var rate = new CoreLogic.Services.Rate.Models.CurrencyRate(
								cur: CurrencyRateType.Gold,
								stamp: DateTime.UtcNow,
								usd: usd
							);
							dispatcher.OnProviderCurrencyRate(rate);

							// send
							dispatcher.Run(TimeSpan.FromSeconds(0.1));
							Thread.Sleep(10000);
						}
					}
					Thread.Sleep(15000);
				}

				testStop.Set();
			});

			// sub part
			var hits = new Dictionary<long, long>();
			var ts = Task.Factory.StartNew(() => {

				using (var sub = new DefaultSubscriber<SafeRatesMessage>(Topic.FiatRates, socket, LogFactory)) {
					sub.SetCallback((subscriber, message) => {
						var rate = message.Rates.First(_ => _.Currency == CurrencyRateType.Gold);

						if (!hits.ContainsKey(rate.Usd)) {
							hits[rate.Usd] = 0;
						}
						hits[rate.Usd]++;
					});
					sub.Run();

					testStop.Wait();
				}
			});

			Task.WaitAll(tp, ts);

			foreach (var pair in hits) {
				Logger.Debug($"Hits at { pair.Key } = { pair.Value }");
			}
			Assert.True(hits[0] > 0);
			Assert.True(hits[1] > 0);
		}*/

		// ---

		[Fact]
		public void DefaultUnsafeRates() {

			var socket = GetNextSocketAddress;

			// subscriber source
			using (var sub = new DefaultSubscriber<SafeRatesMessage>(new []{ Topic.FiatRates }, socket, LogFactory)) {
				using (var source = new BusSafeRatesSource(sub, LogFactory)) {

					var gold = source.GetRate(CurrencyRateType.Gold);
					Assert.True(gold != null);
					Assert.True(gold.Usd == 0);
					Assert.False(gold.CanBuy);
					Assert.False(gold.CanSell);

					var crypto = source.GetRate(CurrencyRateType.Eth);
					Assert.True(crypto != null);
					Assert.True(crypto.Usd == 0);
					Assert.False(crypto.CanBuy);
					Assert.False(crypto.CanSell);
				}
			}

			// dispatcher source
			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();

				var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);
				using (var source = new SafeRatesDispatcher(pubWrapper, LogFactory, null)) {

					var gold = source.GetRate(CurrencyRateType.Gold);
					Assert.True(gold != null);
					Assert.True(gold.Usd == 0);
					Assert.False(gold.CanBuy);
					Assert.False(gold.CanSell);

					var crypto = source.GetRate(CurrencyRateType.Eth);
					Assert.True(crypto != null);
					Assert.True(crypto.Usd == 0);
					Assert.False(crypto.CanBuy);
					Assert.False(crypto.CanSell);
				}
			}
		}

		[Fact]
		public void FreshAndStaleRates() {

			var socket = GetNextSocketAddress;

			using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
				pub.Run();

				using (var sub = new DefaultSubscriber<SafeRatesMessage>(new []{ Topic.FiatRates }, socket, LogFactory)) {
					using (var source = new BusSafeRatesSource(sub, LogFactory)) {
						sub.Connect();

						var stamp = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
						var freshFor = 2;

						// send fresh
						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() {
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
						Assert.True(sub.ReceiveBlocking(out var ratesMessage));
						source.OnNewRates(sub, ratesMessage);

						// got fresh
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF);
						Assert.True(source.GetRate(CurrencyRateType.Gold).CanBuy && source.GetRate(CurrencyRateType.Gold).CanSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE);
						Assert.True(source.GetRate(CurrencyRateType.Eth).CanBuy && source.GetRate(CurrencyRateType.Eth).CanSell);

						// timeout
						Thread.Sleep(freshFor * 1000 + 100);

						// become stale/unsafe
						Assert.True(source.GetRate(CurrencyRateType.Gold).Usd == 0xDEADBEEF);
						Assert.True(!source.GetRate(CurrencyRateType.Gold).CanBuy && !source.GetRate(CurrencyRateType.Gold).CanSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE);
						Assert.True(!source.GetRate(CurrencyRateType.Eth).CanBuy && !source.GetRate(CurrencyRateType.Eth).CanSell);

						// send stale
						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() {
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
						Assert.True(!source.GetRate(CurrencyRateType.Gold).CanBuy && !source.GetRate(CurrencyRateType.Gold).CanSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE + 0xD);
						Assert.True(!source.GetRate(CurrencyRateType.Eth).CanBuy && !source.GetRate(CurrencyRateType.Eth).CanSell);

						// send fresh again
						stamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
						pub.PublishMessage(Topic.FiatRates, new SafeRatesMessage() {
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
						Assert.True(!source.GetRate(CurrencyRateType.Gold).CanBuy && source.GetRate(CurrencyRateType.Gold).CanSell);
						Assert.True(source.GetRate(CurrencyRateType.Eth).Usd == 0xDEADBEEE + 0xDE);
						Assert.True(source.GetRate(CurrencyRateType.Eth).CanBuy && !source.GetRate(CurrencyRateType.Eth).CanSell);
					}
				}
			}
		}

		[Fact]
		public void DispatcherRates() {

			var socket = GetNextSocketAddress;

			var randomRates = new DebugRateProvider();
			var stopCts = new CancellationTokenSource();
			var stopToken = stopCts.Token;

			// pub part
			var tp = Task.Factory.StartNew(() => {
				using (var pub = new DefaultPublisher<SafeRatesMessage>(socket, LogFactory)) {
					pub.Run();

					var pubWrapper = new BusSafeRatesPublisher(pub, LogFactory);
					using (var dispatcher = new SafeRatesDispatcher(pubWrapper, LogFactory, null)) {

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
				BusSafeRatesSource source = null;

				using (var sub = new DefaultSubscriber<SafeRatesMessage>(new []{ Topic.FiatRates }, socket, LogFactory)) {
					sub.Run();

					source = new BusSafeRatesSource(sub, LogFactory);

					var goldOk = false;
					var ethOk = false;
					while (!goldOk && !ethOk) {

						Thread.Sleep(100);

						var goldRate = source.GetRate(CurrencyRateType.Gold);
						var ethRate = source.GetRate(CurrencyRateType.Eth);

						if (goldRate.CanBuy && goldRate.CanSell) {
							goldOk = true;
						}
						if (ethRate.CanBuy && ethRate.CanSell) {
							ethOk = true;
						}
					}

					stopCts.Cancel();
				}
				source?.Dispose();
			});
			
			Task.WaitAll(tp, ts);
			stopCts.Dispose();
		}

		// ---

		// TODO: dispatcher check (rate volatility)
	}
}
