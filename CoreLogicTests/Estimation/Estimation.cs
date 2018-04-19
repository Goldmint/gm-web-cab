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

namespace Goldmint.CoreLogicTests.Estimation {

	public sealed class Estimation : Test {

		public Estimation(ITestOutputHelper testOutput) : base(testOutput) {
		}

		// ---

		// TODO: CoreLogic.Finance.Estimation.* methods - valid/invalid values, valid/invalid rates, allow/disallow trading

		// TODO: fee estimation while selling (mntp balance)
	}
}
