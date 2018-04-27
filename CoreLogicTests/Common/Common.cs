using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Common {

	public sealed class Common : Test {

		public Common(ITestOutputHelper testOutput) : base(testOutput) {
		}

		// ---

		[Fact]
		public void UserIdExtraction() {
			Assert.True(CoreLogic.User.ExtractId("u000001") == 1);
			Assert.True(CoreLogic.User.ExtractId("u666") == 666);
			Assert.Null(CoreLogic.User.ExtractId("666"));
			Assert.Null(CoreLogic.User.ExtractId("uu666"));
			Assert.Null(CoreLogic.User.ExtractId("xhxuiasuhidu"));
			Assert.Null(CoreLogic.User.ExtractId(""));
			Assert.Null(CoreLogic.User.ExtractId("  "));
			Assert.Null(CoreLogic.User.ExtractId(" 			  "));
			Assert.Null(CoreLogic.User.ExtractId(null));
		}
	}
}
