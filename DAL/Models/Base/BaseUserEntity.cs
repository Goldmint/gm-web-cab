using Goldmint.DAL.Models.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models
{

	public abstract class BaseUserEntity : BaseEntity
	{

		[Column("user_id"), Required]
		public long UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public virtual User User { get; set; }
	}
}
