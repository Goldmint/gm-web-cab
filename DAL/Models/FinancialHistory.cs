using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_financial_history")]
	public class FinancialHistory : BaseUserEntity, IConcurrentUpdate {

		[Column("type"), Required]
		public FinancialHistoryType Type { get; set; }

		[Column("status"), Required]
		public FinancialHistoryStatus Status { get; set; }

		[Column("currency"), Required]
		public FiatCurrency Currency { get; set; }

		[Column("amount"), Required]
		public long AmountCents { get; set; }

		[Column("fee"), Required]
		public long FeeCents { get; set; }

		[Column("comment"), MaxLength(512), Required]
		public string Comment { get; set; }

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
