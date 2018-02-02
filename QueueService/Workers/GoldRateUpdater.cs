using Goldmint.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Workers {

	public class GoldRateUpdater : BaseWorker {

		private const long StaleValueInvalidationTimeoutSeconds = 60;

		private static long _usdStamp = 0;
		private static long _usdValue = 0;

		// ---

		private IServiceProvider _services;

		public GoldRateUpdater() {
		}

		protected override Task OnInit(IServiceProvider services) {
			_services = services;
			return Task.CompletedTask;
		}

		protected override async Task Loop() {
			try {

				await UpdateUSD();

			} catch (Exception e) {
				Logger.Error(e);
			}
		}

		// ---

		private Task UpdateUSD() {

			var newValue = 133000 + (SecureRandom.GetPositiveInt() % 6000) - 3000;
			var newStamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

			Interlocked.Exchange(ref _usdValue, newValue);
			Interlocked.Exchange(ref _usdStamp, newStamp);
			
			return Task.CompletedTask;
		}

		// ---

		public static long? GetGoldRate(FiatCurrency currency) {

			var stamp = 0L;
			var value = 0L;

			if (currency == FiatCurrency.USD) {
				stamp = Interlocked.Read(ref _usdStamp);
				value = Interlocked.Read(ref _usdValue);	
			}

			// check
			var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			if (value > 0 && stamp > 0 && Math.Abs(now - stamp) <= StaleValueInvalidationTimeoutSeconds) {
				return value;
			}

			return null;
		}
	}
}
