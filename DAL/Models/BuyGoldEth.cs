using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_buy_gold_eth")]
	public class BuyGoldEth : BaseUserFinHistory, IConcurrentUpdate {

		[Column("status"), Required]
		public BuySellGoldRequestStatus Status { get; set; }
		
		[Column("exchange_currency"), Required]
		public FiatCurrency ExchangeCurrency { get; set; }

		[Column("gold_rate"), Required]
		public long GoldRateCents { get; set; }

		[Column("eth_rate"), Required]
		public long EthRateCents { get; set; }

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
