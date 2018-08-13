using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.Identity {

    [Table("gm_role")]
	public class Role : IdentityRole<long> {

		public Role() : base() { }
		public Role(string roleName) : base(roleName) { }

		// ---

		[Column("id")]
		public override long Id { get; set; }

		[Column("name"), MaxLength(256)]
		public override string Name { get; set; }

		[Column("normalized_name"), MaxLength(256)]
		public override string NormalizedName { get; set; }

		[Column("concurrency_stamp"), MaxLength(64)]
		public override string ConcurrencyStamp { get; set; }
	}
}
