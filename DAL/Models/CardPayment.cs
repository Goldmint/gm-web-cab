using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_card_payment")]
	public class CardPayment : BaseUserEntity, IConcurrentUpdate {

		[Column("card_id"), Required]
		public long CardId { get; set; }

		[ForeignKey(nameof(CardId))]
		public virtual Card Card { get; set; }

		[Column("type"), Required]
		public CardPaymentType Type { get; set; }

		[Column("status"), Required]
		public CardPaymentStatus Status { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("amount"), Required]
		public long AmountCents { get; set; }

		[Column("transaction_id"), MaxLength(32), Required]
		public string TransactionId { get; set; }

		[Column("gw_transaction_id"), MaxLength(64), Required]
		public string GWTransactionId { get; set; }

		[Column("provider_status"), MaxLength(64)]
		public string ProviderStatus { get; set; }

		[Column("provider_message"), MaxLength(512)]
		public string ProviderMessage { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

		[Column("ref_payment_id")]
		public long? RefPaymentId { get; set; }
		[ForeignKey(nameof(RefPaymentId))]
		public virtual CardPayment RefPayment { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_next_check"), Required]
		public DateTime TimeNextCheck { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
