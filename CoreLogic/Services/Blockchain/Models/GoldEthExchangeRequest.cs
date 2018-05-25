using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Models {

	public sealed class GoldEthExchangeRequest {

		/// <summary>
		/// Index
		/// </summary>
		public BigInteger RequestIndex { get; set; }

		/// <summary>
		/// User address
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Request payload
		/// </summary>
		public BigInteger Reference { get; set; }
		
		/// <summary>
		/// Request amount
		/// </summary>
		public BigInteger Amount { get; set; }

		/// <summary>
		/// Is buy request
		/// </summary>
		public bool IsBuyRequest { get; set; }

		/// <summary>
		/// Is pending
		/// </summary>
		public bool IsPending { get; set; }

		/// <summary>
		/// Is succeeded
		/// </summary>
		public bool IsSucceeded { get; set; }

		/// <summary>
		/// Is cancelled
		/// </summary>
		public bool IsCancelled { get; set; }
		
		/// <summary>
		/// Is failed
		/// </summary>
		public bool IsFailed { get; set; }
	}
}
