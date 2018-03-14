using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_user_oplog")]
	public class UserOpLog : BaseUserEntity, IConcurrentUpdate {

		[Column("ref_id")]
		public long? RefId { get; set; }

		[ForeignKey(nameof(RefId))]
		public virtual UserOpLog Ref { get; set; }

		[Column("status"), Required]
		public UserOpLogStatus Status { get; set; }

		[Column("message"), MaxLength(512), Required]
		public string Message { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
