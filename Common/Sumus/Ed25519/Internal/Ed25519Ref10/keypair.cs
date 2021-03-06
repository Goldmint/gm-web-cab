﻿using System;

namespace Goldmint.Common.Sumus.Ed25519.Internal.Ed25519Ref10
{
    internal static partial class Ed25519Operations
    {
        public static void crypto_sign_keypair(byte[] pk, int pkoffset, byte[] sk, int skoffset, byte[] seed, int seedoffset)
        {
            GroupElementP3 A;
            int i;

            Array.Copy(seed, seedoffset, sk, skoffset, 32);
            byte[] h = Sha512.Hash(sk, skoffset, 32);//ToDo: Remove alloc
            ScalarOperations.sc_clamp(h, 0);

            GroupOperations.ge_scalarmult_base(out A, h, 0);
            GroupOperations.ge_p3_tobytes(pk, pkoffset, ref A);

            for (i = 0; i < 32; ++i) sk[skoffset + 32 + i] = pk[pkoffset + i];
            CryptoBytes.Wipe(h);
        }

	    public static void crypto_sign_private_prehash(byte[] sk, byte[] prehashedSk) {
			var prehashed = new byte[64];
			Array.Copy(sk, prehashed, 64);
		    var hasher = new Sha512();
			hasher.Update(sk, 0, 64);
			var h = hasher.Finalize();
			ScalarOperations.sc_clamp(h, 0);
			Array.Copy(h, prehashed, 32);
			Array.Copy(prehashed, prehashedSk, 64);
	    }

	    public static void crypto_sign_keypair_prehashed(byte[] pk, int pkoffset, byte[] sk, int skoffset) {
		    GroupElementP3 A;

		    GroupOperations.ge_scalarmult_base(out A, sk, skoffset);
		    GroupOperations.ge_p3_tobytes(pk, pkoffset, ref A);
	    }
	}
}
