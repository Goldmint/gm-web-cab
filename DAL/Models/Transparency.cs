using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_transparency")]
	public class Transparency : BaseUserEntity {

		[Column("amount"), MaxLength(FieldMaxLength.Comment)]
		public string Amount { get; set; }

		[Column("hash"), MaxLength(FieldMaxLength.TransparencyTransactionHash), Required]
		public string Hash { get; set; }

		[Column("comment"), MaxLength(FieldMaxLength.Comment), Required]
		public string Comment { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }
	}
}
