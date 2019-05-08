using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_user_options")]
	public class UserOptions : BaseUserEntity, IConcurrentUpdate {

		[Column("init_tfa_quest")]
		public bool InitialTfaQuest { get; set; }

		[Column("dpa_document_id")]
		public long? DpaDocumentId { get; set; }

		[ForeignKey(nameof(DpaDocumentId))]
		public virtual SignedDocument DpaDocument { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
