using Goldmint.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	[Table("gm_notification")]
	public class Notification : BaseEntity {

		[Column("type"), Required]
		public NotificationType Type { get; set; }

		[Column("data", TypeName = "TEXT"), Required]
		public string JsonData { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_to_send"), Required]
		public DateTime TimeToSend { get; set; }
	}
}
