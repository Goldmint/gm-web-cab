using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_eth_operation")]
	public class EthereumOperation: BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("type"), Required]
		public EthereumOperationType Type { get; set; }

		[Column("status"), Required]
		public EthereumOperationStatus Status { get; set; }

		[Column("rel_request_id")]
		public long? RelatedExchangeRequestId { get; set; }

		[Column("address"), MaxLength(FieldMaxLength.BlockchainAddress), Required]
		public string DestinationAddress { get; set; }

		[Column("rate"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount), Required]
		public string Rate { get; set; }

		[Column("gold_amount"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount), Required]
		public string GoldAmount { get; set; }

		[Column("eth_request_index"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount)]
		public string EthRequestIndex { get; set; }

		[Column("eth_txid"), MaxLength(FieldMaxLength.EthereumTransactionHash)]
		public string EthTransactionId { get; set; }

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
