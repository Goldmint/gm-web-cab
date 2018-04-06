using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_trans_gold_request")]
	public class TransferGoldRequest : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public TransferGoldRequestStatus Status { get; set; }

		[Column("address"), MaxLength(FieldMaxLength.BlockchainMaxAddress), Required]
		public string DestinationAddress { get; set; }

		[Column("amount_wei"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount), Required]
		public string AmountWei { get; set; }

		[Column("eth_transaction_id"), MaxLength(FieldMaxLength.EthereumTransactionHash)]
		public string EthTransactionId { get; set; }

		[Column("desk_ticket_id"), MaxLength(FieldMaxLength.Guid), Required]
		public string DeskTicketId { get; set; }

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
