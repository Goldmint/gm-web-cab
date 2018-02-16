using Goldmint.DAL.Models.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_banned_country")]
	public class BannedCountry {

		[Key, Column("code"), MaxLength(3), Required]
		public string Code { get; set; }

		[Column("comment"), MaxLength(128)]
		public string Comment { get; set; }

		[Column("user_id"), Required]
		public long UserId { get; set; }
		[ForeignKey(nameof(UserId))]
		public virtual User User { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }
	}
}
