using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.DAL.Models.Identity;

namespace Goldmint.DAL.Models {

	/*
	[Table("gm_cryptexc_request")]
	public class CryptoExchangeRequest : BaseFinancialHistoryEntity, IConcurrentUpdate {

		[Column("type"), Required]
		public CryptoExchangeRequestType Type { get; set; }

		[Column("origin"), Required]
		public CryptoExchangeRequestOrigin Origin { get; set; }

		[Column("status"), Required]
		public CryptoExchangeRequestStatus Status { get; set; }

		[Column("origin_txid"), MaxLength(256), Required]
		public string EthTransactionId { get; set; }

		[Column("address"), MaxLength(128), Required]
		public string Address { get; set; }

		[Column("amount"), MaxLength(256), Required]
		public string Amount { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("rate")]
		public long? RateCents { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

		[Column("support_comment"), MaxLength(512)]
		public string SupportComment { get; set; }

		[Column("support_user_id")]
		public long? SupportUserId { get; set; }
		[ForeignKey(nameof(SupportUserId))]
		public virtual User SupportUser { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_prepared")]
		public DateTime? TimePrepared { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
	*/
}
