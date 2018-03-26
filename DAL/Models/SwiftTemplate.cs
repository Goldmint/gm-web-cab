using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Goldmint.Common;

namespace Goldmint.DAL.Models {

	[Table("gm_swift_template")]
	public class SwiftTemplate : BaseUserEntity, IConcurrentUpdate {

		[Column("name"), MaxLength(64), Required]
		public string Name { get; set; }

		[Column("holder"), MaxLength(256), Required]
		public string Holder { get; set; }

		[Column("iban"), MaxLength(256), Required]
		public string Iban { get; set; }

		[Column("bank"), MaxLength(256), Required]
		public string Bank { get; set; }

		[Column("bic"), MaxLength(128), Required]
		public string Bic { get; set; }

		[Column("details"), MaxLength(1024), Required]
		public string Details { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
