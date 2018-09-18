using Goldmint.DAL.Models.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;

namespace Goldmint.DAL.Models
{

	[Table("gm_promo_code")]
	public class PromoCode : BaseEntity, IConcurrentUpdate
	{
		[Column("code"), MaxLength(32), Required]
		public string Code { get; set; }

	    [Column("token_type"), Required]
	    public CryptoCurrency TokenType { get; set; }

	    [Column("limit"), Required]
	    public long Limit { get; set; }

        [Column("discount_value"), Required]
		public long DiscountValue { get; set; }

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

		public void OnConcurrencyStampRegen()
		{
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
