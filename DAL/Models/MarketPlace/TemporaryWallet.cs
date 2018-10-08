using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.MarketPlace
{
	[Table("gm_temporary_wallet")]
	public class TemporaryWallet : BaseUserEntity, IConcurrentUpdate
	{
	    [Column("public_key"), MaxLength(FieldMaxLength.SumusAddress), Required]
	    public string PublicKey { get; set; }

	    [Column("private_key"), MaxLength(FieldMaxLength.SumusAddress), Required]
	    public string PrivateKey { get; set; }

        [Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen()
		{
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
