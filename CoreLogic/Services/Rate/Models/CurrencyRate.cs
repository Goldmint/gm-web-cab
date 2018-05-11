using Goldmint.Common;
using System;

namespace Goldmint.CoreLogic.Services.Rate.Models {

	public class CurrencyRate {

		public readonly CurrencyRateType Currency;
		public readonly DateTime Stamp;
		public readonly long Usd;

		public CurrencyRate(CurrencyRateType cur, DateTime stamp, long usd) {
			Currency = cur;
			Stamp = stamp;
			Usd = usd;
		}

		public override string ToString() {
			return $"cur={ Currency.ToString() };usd={ Usd };stm={ Stamp };";
		}
	}

	public class SafeCurrencyRate : CurrencyRate {

		private readonly bool _canBuy;
		private readonly bool _canSell;
		private readonly TimeSpan _ttl;

		public SafeCurrencyRate(bool canBuy, bool canSell, TimeSpan ttl, CurrencyRateType cur, DateTime stamp, long usd) : base(cur, stamp, usd) {
			_canBuy = canBuy;
			_canSell = canSell;
			_ttl = ttl;
		}

		public bool CanBuy => _canBuy && !IsExpired;
		public bool CanSell => _canSell && !IsExpired;
		public bool IsExpired => DateTime.UtcNow > Stamp.Add(_ttl);

		public override string ToString() {
			return base.ToString() + $"sfb={ CanBuy };sfs={ CanSell };exp={ IsExpired };";
		}

		// ---

		public static Bus.Proto.SafeRates.Rate BusSerialize(SafeCurrencyRate rate) {
			return new Bus.Proto.SafeRates.Rate() {
				Currency = rate.Currency,
				Stamp = ((DateTimeOffset)rate.Stamp).ToUnixTimeSeconds(),
				Ttl = (long)rate._ttl.TotalSeconds,
				CanBuy = rate._canBuy,
				CanSell = rate._canSell,
				Usd = rate.Usd,
			};
		}

		public static SafeCurrencyRate BusDeserialize(Bus.Proto.SafeRates.Rate rate) {
			return new SafeCurrencyRate(
				canBuy: rate.CanBuy,
				canSell: rate.CanSell,
				ttl: TimeSpan.FromSeconds(rate.Ttl),
				cur: rate.Currency,
				stamp: DateTimeOffset.FromUnixTimeSeconds(rate.Stamp).UtcDateTime,
				usd: rate.Usd
			);
		}
	}
}
