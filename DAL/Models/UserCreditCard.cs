using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_user_ccard")]
	public class UserCreditCard : BaseUserEntity, IConcurrentUpdate {

		[Column("state"), Required]
		public CardState State { get; set; }

		[Column("gw_deposit_card_tid"), MaxLength(FieldMaxLength.The1StPaymentTxId)]
		public string GwInitialDepositCardTransactionId { get; set; }

		[Column("gw_withdraw_card_tid"), MaxLength(FieldMaxLength.The1StPaymentTxId)]
		public string GwInitialWithdrawCardTransactionId { get; set; }

		[Column("mask"), MaxLength(FieldMaxLength.CreditCardMask)]
		public string CardMask { get; set; }

		[Column("holder_name"), MaxLength(FieldMaxLength.CreditCardHolderName)]
		public string HolderName { get; set; }

		[Column("verification_amount"), Required]
		public long VerificationAmountCents { get; set; }

		[Column("verification_attempt"), Required]
		public int VerificationAttempt { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.Guid), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}

}
