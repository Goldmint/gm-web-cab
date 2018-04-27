using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Models {

	public sealed class GatheredGoldBoughtWithEthEvent {

		public BigInteger FromBlock { get; set; }
		public BigInteger ToBlock { get; set; }
		public GoldBoughtWithEthEvent[] Events { get; set; }
	}

	public sealed class GoldBoughtWithEthEvent {

		/// <summary>
		/// User address
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Ethereum amount
		/// </summary>
		public BigInteger EthAmount { get; set; }

		/// <summary>
		/// Request ID
		/// </summary>
		public BigInteger Reference { get; set; }

		/// <summary>
		/// Request index (contract)
		/// </summary>
		public BigInteger RequestIndex { get; set; }

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
