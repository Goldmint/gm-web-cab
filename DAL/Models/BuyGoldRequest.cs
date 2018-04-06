using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_buy_gold_request")]
	public class BuyGoldRequest : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public BuyGoldRequestStatus Status { get; set; }

		[Column("input"), Required]
		public BuyGoldRequestInput Input { get; set; }

		[Column("destination"), Required]
		public BuyGoldRequestDestination Destination { get; set; }

		[Column("destination_address"), MaxLength(FieldMaxLength.BlockchainMaxAddress), Required]
		public string DestinationAddress { get; set; }

		[Column("exchange_currency"), Required]
		public FiatCurrency ExchangeCurrency { get; set; }

		[Column("input_rate"), Required]
		public long InputRateCents { get; set; }

		[Column("gold_rate"), Required]
		public long GoldRateCents { get; set; }

		[Column("desk_ticket_id"), MaxLength(FieldMaxLength.Guid), Required]
		public string DeskTicketId { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_expires"), Required]
		public DateTime TimeExpires { get; set; }

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
