using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_card")]
	public class Card : BaseUserEntity, IConcurrentUpdate {

		[Column("state"), Required]
		public CardState State { get; set; }

		[Column("gw_deposit_card_tid"), MaxLength(64)]
		public string GWInitialDepositCardTransactionId { get; set; }

		[Column("gw_withdraw_card_tid"), MaxLength(64)]
		public string GWInitialWithdrawCardTransactionId { get; set; }

		[Column("card_masked"), MaxLength(64)]
		public string CardMask { get; set; }

		[Column("card_holder"), MaxLength(128)]
		public string CardHolder { get; set; }

		[Column("verification_amount"), Required]
		public long VerificationAmountCents { get; set; }

		[Column("verification_attempt"), Required]
		public int VerificationAttempt { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
