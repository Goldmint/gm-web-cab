using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_transparency")]
	public class Transparency : BaseUserEntity {

		[Column("amount"), Required]
		public long Amount { get; set; }

		[Column("hash"), MaxLength(128), Required]
		public string Hash { get; set; }

		[Column("comment"), MaxLength(512), Required]
		public string Comment { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }
	}
}
