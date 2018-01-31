using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public sealed class ExchangeRequestData {

		/// <summary>
		/// Index
		/// </summary>
		public BigInteger RequestIndex { get; set; }

		/// <summary>
		/// User address
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// User ID
		/// </summary>
		public long UserId { get; set; }

		/// <summary>
		/// Request payload
		/// </summary>
		public string Payload { get; set; }

		/// <summary>
		/// Is buy request
		/// </summary>
		public bool IsBuyRequest { get; set; }

		/// <summary>
		/// Is succeeded
		/// </summary>
		public bool IsSucceeded { get; set; }

		/// <summary>
		/// Is failed
		/// </summary>
		public bool IsCancelled { get; set; }

		/// <summary>
		/// Is pending
		/// </summary>
		public bool IsPending { get; set; }
	}
}
