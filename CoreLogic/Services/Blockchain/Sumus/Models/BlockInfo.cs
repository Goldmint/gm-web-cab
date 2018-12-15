using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Models {

	public sealed class BlockInfo {

		public ulong Id { get; internal set; }
		public ulong Transactions { get; internal set; }
		public ulong TotalUserData { get; internal set; }
		public decimal TotalGold { get; internal set; }
		public decimal TotalMnt { get; internal set; }
		public decimal FeeGold { get; internal set; }
		public decimal FeeMnt { get; internal set; }
		public DateTime TimeStamp { get; internal set; }
	}
}
