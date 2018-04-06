using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_user_finhistory")]
	public class UserFinHistory : BaseUserEntity, IConcurrentUpdate {

		[Column("type"), Required]
		public UserFinHistoryType Type { get; set; }

		[Column("status"), Required]
		public UserFinHistoryStatus Status { get; set; }

		[Column("source"), MaxLength(128), Required]
		public string Source { get; set; }

		[Column("destination"), MaxLength(128)]
		public string Destination { get; set; }

		[Column("comment"), MaxLength(512), Required]
		public string Comment { get; set; }

		[Column("rel_eth_transaction_id"), MaxLength(66)]
		public string RelEthTransactionId { get; set; }

		[Column("desk_ticket_id"), MaxLength(32), Required]
		public string DeskTicketId { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_expires")]
		public DateTime? TimeExpires { get; set; }

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
