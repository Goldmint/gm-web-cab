using Goldmint.DAL.Models.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Goldmint.Common;

namespace Goldmint.DAL.Models {

	[Table("gm_promo_code")]
	public class PromoCode : BaseEntity, IConcurrentUpdate {

		[Column("code"), MaxLength(32), Required]
		public string Code { get; set; }

		[Column("value"), Required]
		public decimal Value { get; set; }

		[Column("user_id")]
		public long? UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public virtual User User { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_expires"), Required]
		public DateTime TimeExpires { get; set; }

		[Column("time_used")]
		public DateTime? TimeUsed { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
