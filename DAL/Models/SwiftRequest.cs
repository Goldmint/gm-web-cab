using Goldmint.Common;
using Goldmint.DAL.Models.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_swift_request")]
	public class SwiftRequest : BaseUserEntity, IConcurrentUpdate {

		[Column("type"), Required]
		public SwiftPaymentType Type { get; set; }

		[Column("status"), Required]
		public SwiftPaymentStatus Status { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("amount"), Required]
		public long AmountCents { get; set; }

		[Column("holder"), MaxLength(256), Required]
		public string Holder { get; set; }

		[Column("holder_address"), MaxLength(512), Required]
		public string HolderAddress { get; set; }

		[Column("iban"), MaxLength(256), Required]
		public string Iban { get; set; }

		[Column("bank"), MaxLength(256), Required]
		public string Bank { get; set; }

		[Column("bic"), MaxLength(128), Required]
		public string Bic { get; set; }

		[Column("details"), MaxLength(1024), Required]
		public string Details { get; set; }

		[Column("payment_ref"), MaxLength(128), Required]
		public string PaymentReference { get; set; }

		[Column("support_comment"), MaxLength(512)]
		public string SupportComment { get; set; }

		[Column("support_user_id")]
		public long? SupportUserId { get; set; }
		[ForeignKey(nameof(SupportUserId))]
		public virtual User SupportUser { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

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
