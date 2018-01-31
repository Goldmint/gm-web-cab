using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_user_activity")]
	public class UserActivity : BaseUserEntity {

		[Column("type"), MaxLength(32), Required]
		public string Type { get; set; }

		[Column("comment"), MaxLength(512), Required]
		public string Comment { get; set; }

		[Column("ip"), MaxLength(32), Required]
		public string Ip { get; set; }

		[Column("agent"), MaxLength(128), Required]
		public string Agent { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }
	}
}
