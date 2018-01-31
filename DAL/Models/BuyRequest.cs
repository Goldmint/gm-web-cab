using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_buy_request")]
	public class BuyRequest : BaseUserEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public ExchangeRequestStatus Status { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("fiat_amount"), Required]
		public long FiatAmountCents { get; set; }

		[Column("address"), MaxLength(64), Required]
		public string Address { get; set; }

		[Column("fixed_rate"), Required]
		public long FixedRateCents { get; set; }

		[Column("actual_rate")]
		public long? ActualRateCents { get; set; }

		[Column("request_index"), MaxLength(64)]
		public string RequestIndex { get; set; }

		[Column("eth_transaction_id"), MaxLength(66)]
		public string EthTransactionId { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_requested")]
		public DateTime? TimeRequested { get; set; }

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
