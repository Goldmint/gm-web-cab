using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_sell_gold_eth")]
	public class SellGoldEth : BaseUserFinHistoryEntity, IConcurrentUpdate {

		[Column("status"), Required]
		public SellGoldRequestStatus Status { get; set; }
		
		[Column("gold_amount"), Required]
		public decimal GoldAmount { get; set; }

		[Column("destination"), MaxLength(FieldMaxLength.BlockchainAddress), Required]
		public string Destination { get; set; }

		[Column("eth_amount"), Required]
		public decimal EthAmount { get; set; }

		[Column("exchange_currency"), Required]
		public FiatCurrency ExchangeCurrency { get; set; }

		[Column("gold_rate"), Required]
		public long GoldRateCents { get; set; }

		[Column("eth_rate"), Required]
		public long OutputRateCents { get; set; }

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
