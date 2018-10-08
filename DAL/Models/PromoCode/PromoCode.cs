using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;

namespace Goldmint.DAL.Models.PromoCode
{
	[Table("gm_promo_code")]
	public class Pawn : BaseEntity, IConcurrentUpdate
	{
		[Column("code"), MaxLength(32), Required]
		public string Code { get; set; }

	    [Column("token_type"), Required]
	    public EthereumToken Currency { get; set; }

	    [Column("limit"), Required]
	    public decimal Limit { get; set; }

        [Column("discount_value"), Required]
		public double DiscountValue { get; set; }

	    [Column("usage_type"), Required]
	    public PromoCodeUsageType UsageType { get; set; }
        
		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_expires"), Required]
		public DateTime TimeExpires { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen()
		{
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
