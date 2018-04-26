using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Models {

	public sealed class GatheredGoldSoldForEthEvent {

		public BigInteger FromBlock { get; set; }
		public BigInteger ToBlock { get; set; }
		public GoldSoldForEthEvent[] Events { get; set; }
	}

	public sealed class GoldSoldForEthEvent {

		/// <summary>
		/// User address
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// GOLD amount
		/// </summary>
		public BigInteger GoldAmount { get; set; }

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
