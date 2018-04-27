using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Blockchain.Models {

	public sealed class TransactionInfo {

		public EthTransactionStatus Status { get; internal set; }
		public DateTime? Time { get; internal set; }
	}
}
