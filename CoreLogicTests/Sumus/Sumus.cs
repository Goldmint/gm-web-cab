using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Goldmint.Common.Sumus;
using Xunit;
using Xunit.Abstractions;

namespace Goldmint.CoreLogicTests.Sumus {

	public sealed class Sumus : Test {

		public Sumus(ITestOutputHelper testOutput) : base(testOutput) {
		}

		// ---

		[Fact]
		public void Pack58() {

			// pack/unpack
			var test = new byte[] {0x1, 0x2, 0x3};
			var b58 = Goldmint.Common.Sumus.Pack58.Pack(test);
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack(b58, out var bytes));
			Assert.True(bytes.SequenceEqual(test));

			// unpack
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("qgQdnYdmnhXmA9N7hDHYVTx1BBmCDpeVnpNb5A8mkBt66PDF4", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("RRAeE4H6wMcoYyG3Lymi6UY5VyeupXXgxQrnWFXvgrcqbKwwn", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("2C1LhVBGsNrYgYo32ebGZLuQsUXtB9MohWP9ohyoe9DgvJEfmg", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("k1yMXnDxUAfHDiGHT2xQrgGU9f6rvBtBuVfcSQi9YQwVXAn5P", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("28VY5m11HKiiV7q9J12rQHqYGJKfbrLah8KmNBeaAGgZKmtBCu", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("2tZWtWnzPSwwQfsdm5x7TWcfsDfvRfH1hGkfsexAnxmRCS4ybn", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("Ys1Tjpn2sft5ktbc6rpjbMdyqThEa49nTH4ij5VMouvwJAQG", out _));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("2VE3sWZsGF8kypaP7SXam96rTnxbh7GQLwPikFgbZdMYNEwSx2", out _));
			Assert.False(Goldmint.Common.Sumus.Pack58.Unpack("RRAeE4H6wMcoYyG3Lymi6UY5VyeupXXgxQrnWFXvgrcqbKwwo", out _));
			Assert.False(Goldmint.Common.Sumus.Pack58.Unpack("Qyd7MtJViy8uUzEUb7UW1oqziXSJYUcVi84xtkZHcKicmHEcH", out _));
			Assert.False(Goldmint.Common.Sumus.Pack58.Unpack("RRAeE4H6wMcoYyG3Lymi6UY5VyxupXXgxQrnWFXvgrcqbKwwn", out _));
		}

		[Fact]
		public void UniqueSignerGeneration() {
			Assert.False((new Signer()).PrivateKey == new Signer().PrivateKey);
		}

		[Fact]
		public void SignerFromPrehashedPrivateKey() {
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("TBzyWv8Dga5aN4Hai2nFTwyTXvDJKkJhq8HMDPC9zqTWLSTLo4jFFKKnVS52a1kp7YJdm2b8HrR2Buk9PqyD1DwhxUzsJ", out var pvt));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("2p6QCcwAMLSSXfFFVQT4vYCe8VPwm3rvK4zdNGAM7zeLBqrVLW", out var pub));
			var sig = new Signer(pvt);
			Assert.True(sig.PrivateKeyBytes.SequenceEqual(pvt));
			Assert.True(sig.PublicKeyBytes.SequenceEqual(pub));

			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("4CdzVBba43H7B12zNoSCE8dz8RM9ggUSagfxPdZ1kQ7hbrXLqNNUwGQiiV1VxU3xuEcj4ybxTZPnjq8BAhBUuJxzU8XxQ", out pvt));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("2PztA94iHZdeX8d5hPJbQfUGcN6WWUhfmU6G5ySJQ9cnUueiuk", out pub));
			sig = new Signer(pvt);
			Assert.True(sig.PrivateKeyBytes.SequenceEqual(pvt));
			Assert.True(sig.PublicKeyBytes.SequenceEqual(pub));
		}

		[Fact]
		public void SignVerify() {
			var sig = new Signer();
			var message = new byte[128];
			using (var rnd = new RNGCryptoServiceProvider()) {
				rnd.GetBytes(message);
			}
			var signature = sig.Sign(message);
			Assert.True(Signer.Verify(sig.PublicKeyBytes, message, signature));
		}
	}
}
