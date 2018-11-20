using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Models {

	public sealed class SentTransaction {
		public string Digest { get; set; }
		public ulong Nonce { get; set; }
	}
}
