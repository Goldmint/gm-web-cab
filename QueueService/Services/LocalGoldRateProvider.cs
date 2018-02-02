using Goldmint.CoreLogic.Services.Rate;
using System;
using System.Collections.Generic;
using System.Text;
using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.QueueService.Services {

	public class LocalGoldRateProvider : IGoldRateProvider {

		public Task<long> GetGoldRate(FiatCurrency currency) {
			var value = Workers.GoldRateUpdater.GetGoldRate(FiatCurrency.USD);
			if (value == null) {
				throw new Exception("Failed ot currency not implemented");
			}
			return Task.FromResult(value.Value);
		}
	}
}
