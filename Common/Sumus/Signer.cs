using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Goldmint.Common.Sumus {

	public class Signer {

		public readonly byte[] PrivateKeyBytes;
		public readonly byte[] PublicKeyBytes;
		public readonly string PrivateKey;
		public readonly string PublicKey;
		private ReaderWriterLockSlim _nonceLocker = new ReaderWriterLockSlim();
		private ulong _nonce = 0;

		// New random signer
		public Signer(ulong nonce = 0) {
			var seed = new byte[32];
			using (var rnd = new RNGCryptoServiceProvider()) {
				rnd.GetBytes(seed);
			}
			Ed25519.Ed25519.PrehashedKeyPairFromSeed(out _, out PrivateKeyBytes, seed);
			Ed25519.Ed25519.PublicKeyFromPrehashedPrivateKey(out PublicKeyBytes, PrivateKeyBytes);
			PrivateKey = Pack58.Pack(PrivateKeyBytes);
			PublicKey = Pack58.Pack(PublicKeyBytes);

			_nonce = nonce;
		}

		// New signer from prehashed private key
		public Signer(byte[] privateKey, ulong nonce = 0) {
			if ((privateKey?.Length ?? 0) != 64) {
				throw new ArgumentException("Private key should be 64 bytes length (pre-hashed)");
			}

			PrivateKeyBytes = new byte[64];
			Ed25519.Ed25519.PublicKeyFromPrehashedPrivateKey(out PublicKeyBytes, privateKey);
			Array.Copy(privateKey, PrivateKeyBytes, 64);

			PrivateKey = Pack58.Pack(PrivateKeyBytes);
			PublicKey = Pack58.Pack(PublicKeyBytes);

			_nonce = nonce;
		}

		// ---

		public ulong NextNonce() {
			_nonceLocker.EnterWriteLock();
			ulong ret = _nonce;
			_nonce++;
			_nonceLocker.ExitWriteLock();
			return ret;
		}

		public byte[] Sign(byte[] message) {
			if ((message?.Length ?? 0) == 0) {
				throw new ArgumentException("Can't sign empty message");
			}
			return Ed25519.Ed25519.SignWithPrehashed(message, PrivateKeyBytes, PublicKeyBytes);
		}

		public static bool Verify(byte[] addr, byte[] message, byte[] signature) {
			if (addr == null || message == null || signature == null || addr.Length != 32 || message.Length == 0 || signature.Length != 64) {
				throw new ArgumentException("Invalid address, payload or signature specified");
			}
			return Ed25519.Ed25519.Verify(signature, message, addr);
		}
	}
}
