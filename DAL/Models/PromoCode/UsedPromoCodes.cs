using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.PromoCode
{
	[Table("gm_used_promo_codes")]
	public class UsedPromoCodes : BaseUserEntity, IConcurrentUpdate
    {
        [Column("promo_code_id"), Required]
        public long PromoCodeId { get; set; }

        [ForeignKey(nameof(PromoCodeId))]
        public virtual Pawn PromoCode { get; set; }

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
