using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	public abstract class BaseUserLoggingEntity : BaseUserEntity {

		[Column("oplog_id"), MaxLength(FieldMaxLength.Guid), Required]
		public string OplogId { get; set; }

	}
}
