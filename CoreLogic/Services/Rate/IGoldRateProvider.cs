﻿using Goldmint.CoreLogic.Services.Rate.Models;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Rate {

	public interface IGoldRateProvider {

		Task<GoldRate> RequestGoldRate(TimeSpan timeout);
	}
}
