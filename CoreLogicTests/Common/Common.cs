using System;
using System.Collections.Generic;
using System.Numerics;
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

		[Fact]
		public void TextFormatter() {

			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("-100"), 18) == "0");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("0"), 18) == "0");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("1"), 18) == "0.000000000000000001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("666000000000000000001"), 18) == "666.000000000000000001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("666000000000000000000"), 18) == "666");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("666500000000000000000"), 18) == "666.5");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("1"), 8) == "0.00000001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("100000000"), 8) == "1");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmount(BigInteger.Parse("33306600000"), 8) == "333.066");

			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("-100"), 18) == "0");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("0"), 18, 6) == "0.000000");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("1"), 18, 5) == "0.00000");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("666000000000000000001"), 18, 6) == "666.000000");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("666000000000000000000"), 18, 6) == "666.000000");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("666500001000000000000"), 18, 6) == "666.500001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("1"), 8, 8) == "0.00000001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("100000123"), 8, 6) == "1.000001");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("33306600000"), 8, 9) == "0");
			Assert.True(Goldmint.Common.TextFormatter.FormatTokenAmountFixed(BigInteger.Parse("33306690000"), 8, 4) == "333.0669");
		}
	}
}
