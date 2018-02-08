using Goldmint.Common;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate.Impl {

	public class DebugGoldRateProvider : IGoldRateProvider {

		public Task<long> GetGoldRate(FiatCurrency currency) {
			return Task.FromResult(133000L + (SecureRandom.GetPositiveInt() % 6000) - 3000);
		}
	}
}
