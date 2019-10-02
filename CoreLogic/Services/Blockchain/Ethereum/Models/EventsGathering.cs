using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models.ContractEvent;
using System.Numerics;

namespace Goldmint.CoreLogic.Services.Blockchain.Ethereum.Models {

	public sealed class GatheredPoolFreezerEvents {

		public BigInteger FromBlock { get; set; }
		public BigInteger ToBlock { get; set; }
		public PoolFreezeEvent[] Events { get; set; }
	}
}
