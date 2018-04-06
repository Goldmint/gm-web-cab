using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.DAL.Models {

    public static class FieldMaxLength {

	    public const int BlockchainCurrencyAmount = 64;
	    public const int BlockchainMaxAddress = 256;
		public const int EthereumTransactionHash = 66;

		public const int Guid = 32;
		public const int Ip = 32;
		public const int ConcurrencyStamp = 64;
	    public const int Comment = 512;
	    public const int UserAgent = 128;
    }
}
