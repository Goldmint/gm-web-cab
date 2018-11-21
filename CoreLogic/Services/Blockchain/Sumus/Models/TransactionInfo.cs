using Goldmint.Common;
using System;
using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Models {

	public sealed class TransactionInfo {

		public SumusTransactionStatus Status { get; internal set; }
		public TransactionData Data { get; internal set; }
	}

	public sealed class TransactionData {

		public string Digest { get; internal set; }
		public ulong BlockNumber { get; internal set; }
		public SumusToken Token { get; internal set; }
		public decimal TokenAmount { get; internal set; }
		public string From { get; internal set; }
		public string To { get; internal set; }
		public DateTime TimeStamp { get; internal set; }
	}
}
