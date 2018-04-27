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

		[Column("output"), Required]
		public BuyGoldRequestOutput Output { get; set; }

		[Column("address"), MaxLength(FieldMaxLength.BlockchainAddress), Required]
		public string InputAddress { get; set; }

		[Column("exchange_currency"), Required]
		public FiatCurrency ExchangeCurrency { get; set; }

		[Column("input_rate"), Required]
		public long InputRateCents { get; set; }

		[Column("gold_rate"), Required]
		public long GoldRateCents { get; set; }

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
