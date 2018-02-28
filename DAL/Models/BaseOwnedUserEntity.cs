using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	public abstract class BaseOwnedUserEntity : BaseUserEntity {

		[Column("ownshp_owner")]
		public long? OwnershipOwnerId { get; set; }

		[Column("ownshp_expires")]
		public DateTime? OwnershipExpires { get; set; }
	}
}
