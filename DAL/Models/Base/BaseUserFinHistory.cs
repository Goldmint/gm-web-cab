using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	public abstract class BaseUserFinHistory : BaseUserEntity {

		[Column("rel_finhistory_id")]
		public long? RelFinHistoryId { get; set; }

		[ForeignKey(nameof(RelFinHistoryId))]
		public virtual UserFinHistory RelFinHistory { get; set; }
	}
}
