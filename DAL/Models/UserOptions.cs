using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_user_options")]
	public class UserOptions : BaseUserEntity, IConcurrentUpdate {

		[Column("initial_tfa_quest"), Required]
		public bool InitialTFAQuest { get; set; }

		[Column("primary_agreement_read"), Required]
		public bool PrimaryAgreementRead { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
