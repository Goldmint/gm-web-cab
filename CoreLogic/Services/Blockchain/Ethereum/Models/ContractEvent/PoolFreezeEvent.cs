using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models.ContractEvent {
	
	public sealed class PoolFreezeEvent {

		/// <summary>
		/// User address
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Sumus address
		/// </summary>
		public string SumusAddress { get; set; }

		/// <summary>
		/// Amount (input amount)
		/// </summary>
		public BigInteger Amount { get; set; }

		// ---

		/// <summary>
		/// Block number
		/// </summary>
		public BigInteger BlockNumber { get; set; }

		/// <summary>
		/// Transaction ID
		/// </summary>
		public string TransactionId { get; set; }
	}
}
