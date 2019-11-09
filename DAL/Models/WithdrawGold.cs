using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_withdraw_gold")]
	public class WithdrawGold : BaseUserFinHistory, IConcurrentUpdate {

		[Column("status"), Required]
		public EmissionRequestStatus Status { get; set; }

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
