using Goldmint.DAL.Models.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_banned_country")]
	public class BannedCountry : BaseUserEntity {

		[Column("code"), MaxLength(3), Required]
		public string Code { get; set; }

		[Column("comment"), MaxLength(FieldMaxLength.Comment), Required]
		public string Comment { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }
	}
}
