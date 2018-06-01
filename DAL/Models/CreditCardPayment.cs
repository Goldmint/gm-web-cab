using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Goldmint.Common;

namespace Goldmint.DAL.Models {

	[Table("gm_ccard_payment")]
	public class CreditCardPayment : BaseUserLoggingEntity, IConcurrentUpdate {

		[Column("card_id"), Required]
		public long CardId { get; set; }

		[ForeignKey(nameof(CardId))]
		public virtual UserCreditCard CreditCard { get; set; }

		[Column("type"), Required]
		public CardPaymentType Type { get; set; }

		[Column("status"), Required]
		public CardPaymentStatus Status { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("amount"), Required]
		public long AmountCents { get; set; }

		[Column("transaction_id"), MaxLength(FieldMaxLength.Guid), Required]
		public string TransactionId { get; set; }

		[Column("gw_transaction_id"), MaxLength(FieldMaxLength.The1StPaymentTxId), Required]
		public string GwTransactionId { get; set; }

		[Column("provider_status"), MaxLength(FieldMaxLength.The1StPaymentStatus)]
		public string ProviderStatus { get; set; }

		[Column("provider_message"), MaxLength(FieldMaxLength.Comment)]
		public string ProviderMessage { get; set; }

		[Column("rel_payment_id")]
		public long? RelPaymentId { get; set; }

		[ForeignKey(nameof(RelPaymentId))]
		public virtual CreditCardPayment RefPayment { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_next_check"), Required]
		public DateTime TimeNextCheck { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.Guid), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}

}
