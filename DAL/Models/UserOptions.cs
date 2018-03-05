using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_user_options")]
	public class UserOptions : BaseUserEntity, IConcurrentUpdate {

		[Column("init_tfa_quest")]
		public bool InitialTFAQuest { get; set; }

		[Column("dpa_document_id")]
		public long? DPADocumentId { get; set; }
		[ForeignKey(nameof(DPADocumentId))]
		public virtual SignedDocument DPADocument { get; set; }

		[Column("hw_buying_stamp")]
		public DateTime? HotWalletBuyingLastTime { get; set; }

		[Column("hw_selling_stamp")]
		public DateTime? HotWalletSellingLastTime { get; set; }

		[Column("hw_transfer_stamp")]
		public DateTime? HotWalletTransferLastTime { get; set; }

		[Column("concurrency_stamp"), MaxLength(64), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
