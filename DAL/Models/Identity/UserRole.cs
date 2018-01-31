using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.Identity {

	public class UserRole : IdentityUserRole<long> {

		public UserRole() : base() { }

		// ---

		[Column("user_id")]
		public override long UserId { get; set; }

		[Column("role_id")]
		public override long RoleId { get; set; }
	}
}
