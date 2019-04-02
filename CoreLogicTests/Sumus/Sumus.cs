using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Goldmint.Common;
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

		[Fact]
		public void Serializer() {

			using (var s = new Serializer()) {
				s.Write("asdasd語でゼ");
			}

			try {
				using (var s = new Serializer()) {
					s.Write("言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言言語でゼロ埋め言");
				}
				Assert.True(false);
			}
			catch {
			}

			using (var s = new Serializer()) {
				s.WriteAmount(BigInteger.Parse("1234000000000000001234"));
				Assert.True(s.Hex() == "003412000000000000003412000000");
			}

			using (var s = new Serializer()) {
				s.WriteAmount(BigInteger.Parse("-123400000000004321"));
				Assert.True(s.Hex() == "012143000000000034120000000000");
			}

			using (var s = new Serializer()) {
				s.WriteAmount(BigInteger.Parse("1123456789123456789"));
				Assert.True(s.Hex() == "008967452391785634120100000000");
			}
		}

		[Fact]
		public void Deserializer() {

			var b = (byte) 142;
			var u16 = (ushort) 0xDEAD;
			var u32 = (uint) 0xDEADBEEF;
			var u64 = (ulong) 0xDEADBEEF1337C0DE;
			var str64 = "961D2014E3E93AC701A6A5F25824DB66";
			var str64Full = "1EF8C0F73B2370D14330C487A70618E0333EAEBA8313EC87131B8F67D964D097";
			var amo1 = BigInteger.Parse("1234567890123456789123456789");
			var amo2 = BigInteger.Parse("-987654321102030405060708090");
			var amo3 = BigInteger.Parse("1000000000000000000000");
			var amo4 = BigInteger.Parse("1000000000000000000");
			var amo5 = BigInteger.Parse("0");

			byte[] bytes;
			using (var s = new Serializer()) {
				s.Write(b);
				s.Write(u16);
				s.Write(u32);
				s.Write(u64);
				s.Write(str64);
				s.Write(str64Full);
				s.WriteAmount(amo1);
				s.WriteAmount(amo2);
				s.WriteAmount(amo3);
				s.WriteAmount(amo4);
				s.WriteAmount(amo5);
				bytes = s.Data();
			}

			using (var d = new Deserializer(bytes)) {
				Assert.True(d.ReadByte(out var xb) && xb == b);
				Assert.True(d.ReadUint16(out var xu16) && xu16 == u16);
				Assert.True(d.ReadUint32(out var xu32) && xu32 == u32);
				Assert.True(d.ReadUint64(out var xu64) && xu64 == u64);
				Assert.True(d.ReadString(out var xstr64) && xstr64 == str64);
				Assert.True(d.ReadString(out var xstr64Full) && xstr64Full == str64Full);
				Assert.True(d.ReadAmount(out var xamo1) && xamo1 == amo1);
				Assert.True(d.ReadAmount(out var xamo2) && xamo2 == amo2);
				Assert.True(d.ReadAmount(out var xamo3) && xamo3 == amo3);
				Assert.True(d.ReadAmount(out var xamo4) && xamo4 == amo4);
				Assert.True(d.ReadAmount(out var xamo5) && xamo5 == amo5);
			}
		}

		[Fact]
		public void TransferAssetTransaction() {

			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("TBzyWv8Dga5aN4Hai2nFTwyTXvDJKkJhq8HMDPC9zqTWLSTLo4jFFKKnVS52a1kp7YJdm2b8HrR2Buk9PqyD1DwhxUzsJ", out var srcpk));
			Assert.True(Goldmint.Common.Sumus.Pack58.Unpack("FhM2u3UMtexZ3TU57G6d9iDpcmynBSpzmTZq6YaMPeA6DHFdEht3jcZUDpXyVbXGoXoWiYB9z8QVKjGhZuKCqMGYZE2P6", out var dstpk));

			var src = new Signer(srcpk);
			var dst = new Signer(dstpk);

			var tx = Transaction.TransferAsset(src, 3, dst.PublicKeyBytes, 0, BigInteger.Parse("1000000000000000000000"));

			Assert.True(tx.Data == "03000000000000000000eea0728dfee30d6a65ff2e5c07ddbc4c304cc9005abe2640822adc1ec944201df42378223753e3f5410b427d4c49df8dee069d798eb5cfb0a4e3bd197b0797b7000000000000000000000010000000010e4b042527eafe9f5c8d90da41d4e062fd044a84e3c1dbcda9342b4921798d9ee56310dda763c137e0ec4e521d2738249120edc7149018eb15240ba373e6090a");
		}
	}
}
