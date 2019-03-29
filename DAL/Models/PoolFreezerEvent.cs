using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_pool_freeze_request")]
	public class PoolFreezeRequest : BaseEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public EmissionRequestStatus Status { get; set; }

		[Column("eth_address"), MaxLength(FieldMaxLength.EthereumAddress), Required]
		public string EthAddress { get; set; }

		[Column("eth_txid"), MaxLength(FieldMaxLength.EthereumTransactionHash), Required]
		public string EthTransaction { get; set; }

		[Column("sum_address"), MaxLength(FieldMaxLength.SumusAddress), Required]
		public string SumAddress { get; set; }

		[Column("amount"), Required]
		public decimal Amount { get; set; }

		[Column("sum_txid"), MaxLength(FieldMaxLength.SumusTransactionHash)]
		public string SumTransaction { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

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
