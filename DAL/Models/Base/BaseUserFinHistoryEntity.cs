using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	public abstract class BaseUserFinHistoryEntity : BaseUserEntity {

		[Column("rel_user_finhistory"), Required]
		public long RelUserFinHistoryId { get; set; }

		[ForeignKey(nameof(RelUserFinHistoryId))]
		public virtual UserFinHistory RelUserFinHistory { get; set; }

	}
}
