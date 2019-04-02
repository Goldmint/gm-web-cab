using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_eth_sending")]
	public class EthSending : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public EthereumOperationStatus Status { get; set; }

		[Column("address"), MaxLength(FieldMaxLength.BlockchainAddress), Required]
		public string Address { get; set; }

		[Column("amount"), Required]
		public decimal Amount { get; set; }

		[Column("tx"), MaxLength(FieldMaxLength.EthereumTransactionHash)]
		public string Transaction { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_next_check"), Required]
		public DateTime TimeNextCheck { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
