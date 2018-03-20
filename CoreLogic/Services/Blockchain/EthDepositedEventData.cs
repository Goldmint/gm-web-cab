using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain {

	public sealed class EthDepositedEventData {

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
		public BigInteger RequestId { get; set; }

		/// <summary>
		/// Block number
		/// </summary>
		public BigInteger BlockNumber { get; set; }

		/// <summary>
		/// Transaction ID
		/// </summary>
		public string TransactionId { get; set; }

		/// <summary>
		/// Current latest block on blockchain
		/// </summary>
		public BigInteger BlockchainLatestBlock { get; set; }
	}
}
