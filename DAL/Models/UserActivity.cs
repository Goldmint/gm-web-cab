using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;

namespace Goldmint.DAL.Models {

	[Table("gm_user_activity")]
	public class UserActivity : BaseUserEntity {

		[Column("type"), MaxLength(32), Required]
		public string Type { get; set; }

		[Column("comment"), MaxLength(FieldMaxLength.Comment), Required]
		public string Comment { get; set; }

		[Column("ip"), MaxLength(FieldMaxLength.Ip), Required]
		public string Ip { get; set; }

		[Column("agent"), MaxLength(FieldMaxLength.UserAgent), Required]
		public string Agent { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("locale")]
		public Locale? Locale { get; set; }
	}
}
