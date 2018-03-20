using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.DAL.Models.Identity;

namespace Goldmint.DAL.Models {

	[Table("gm_crypto_deposit")]
	public class CryptoDeposit : BaseFinancialHistoryEntity, IConcurrentUpdate {

		[Column("origin"), Required]
		public CryptoDepositOrigin Origin { get; set; }

		[Column("status"), Required]
		public CryptoDepositStatus Status { get; set; }

		[Column("address"), MaxLength(128), Required]
		public string Address { get; set; }

		[Column("requested_amount"), MaxLength(64), Required]
		public string RequestedAmount { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("rate"), Required]
		public long RateCents { get; set; }

		[Column("origin_txid"), MaxLength(256)]
		public string TransactionId { get; set; }
		
		[Column("amount"), MaxLength(128)]
		public string Amount { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

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
}
