using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	public abstract class BaseUserFinHistoryEntity : BaseUserLoggingEntity {

		[Column("ref_user_finhistory"), Required]
		public long RefUserFinHistoryId { get; set; }

		[ForeignKey(nameof(RefUserFinHistoryId))]
		public virtual UserFinHistory RefUserFinHistory { get; set; }

	}
}
