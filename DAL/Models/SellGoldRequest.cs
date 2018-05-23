using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_sell_gold_request")]
	public class SellGoldRequest : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public SellGoldRequestStatus Status { get; set; }

		[Column("input"), Required]
		public SellGoldRequestInput Input { get; set; }

		[Column("output"), Required]
		public SellGoldRequestOutput Output { get; set; }

		[Column("rel_output_id")]
		public long? RelOutputId { get; set; }

		[Column("eth_address"), MaxLength(FieldMaxLength.BlockchainAddress), Required]
		public string EthAddress { get; set; }

		[Column("exchange_currency"), Required]
		public FiatCurrency ExchangeCurrency { get; set; }

		[Column("output_rate"), Required]
		public long OutputRateCents { get; set; }

		[Column("gold_rate"), Required]
		public long GoldRateCents { get; set; }

		[Column("input_expected"), MaxLength(FieldMaxLength.BlockchainCurrencyAmount), Required]
		public string InputExpected { get; set; }

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
