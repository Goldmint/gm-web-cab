using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_user_sumus_wallet")]
	public class UserSumusWallet : BaseUserEntity, IConcurrentUpdate {

		[Column("public_key"), MaxLength(FieldMaxLength.SumusAddress), Required]
		public string PublicKey { get; set; }

		[Column("private_key"), MaxLength(FieldMaxLength.SumusAddress), Required]
		public string PrivateKey { get; set; }

		[Column("balance_gold")]
		public decimal BalanceGold { get; set; }

		[Column("balance_mnt")]
		public decimal BalanceMnt { get; set; }

		[Column("tracking")]
		public bool Tracking { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_checked"), Required]
		public DateTime TimeChecked { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
