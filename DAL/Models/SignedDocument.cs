using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;

namespace Goldmint.DAL.Models {

	[Table("gm_signed_document")]
	public class SignedDocument : BaseUserEntity {

		[Column("type"), Required]
		public SignedDocumentType Type { get; set; }

		[Column("is_signed"), Required]
		public bool IsVerified { get; set; }

		[Column("reference_id"), Required, MaxLength(32)]
		public string ReferenceId { get; set; }

		[Column("callback_status"), MaxLength(16)]
		public string CallbackStatus { get; set; }

		[Column("callback_event_type"), MaxLength(64)]
		public string CallbackEvent { get; set; }

		[Column("token"), Required, MaxLength(64)]
		public string Token { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_completed")]
		public DateTime TimeCompleted { get; set; }
	}
}
