using Goldmint.Common;
using Goldmint.DAL.Models.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_sell_gold_crypto_sup")]
	public class SellGoldCryptoSupportRequest : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public SupportRequestStatus Status { get; set; }

		[Column("amount"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount), Required]
		public string AmountWei { get; set; }

		[Column("sell_gold_request_id"), Required]
		public long SellGoldRequestId { get; set; }
		[ForeignKey(nameof(SellGoldRequestId))]
		public virtual SellGoldRequest SellGoldRequest { get; set; }

		[Column("support_comment"), MaxLength(FieldMaxLength.Comment)]
		public string SupportComment { get; set; }

		[Column("support_user_id")]
		public long? SupportUserId { get; set; }
		[ForeignKey(nameof(SupportUserId))]
		public virtual User SupportUser { get; set; }

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
