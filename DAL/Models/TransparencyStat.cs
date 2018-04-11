using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

	[Table("gm_transparency_stat")]
	public class TransparencyStat : BaseUserEntity {

		[Column("assets"), MaxLength(MaxJsonFieldLength), Required]
		public string AssetsArray { get; set; }

		[Column("bonds"), MaxLength(MaxJsonFieldLength), Required]
		public string BondsArray { get; set; }

		[Column("fiat"), MaxLength(MaxJsonFieldLength), Required]
		public string FiatArray { get; set; }

		[Column("gold"), MaxLength(MaxJsonFieldLength), Required]
		public string GoldArray { get; set; }

		[Column("total_oz"), MaxLength(MaxTotalFieldLength), Required]
		public string TotalOz { get; set; }

		[Column("total_usd"), MaxLength(MaxTotalFieldLength), Required]
		public string TotalUsd { get; set; }

		[Column("data_timestamp"), Required]
		public DateTime DataTimestamp { get; set; }

		[Column("audit_timestamp"), Required]
		public DateTime AuditTimestamp { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		// ---

		public const int MaxJsonFieldLength = 2048;
		public const int MaxTotalFieldLength = 128;
	}
}
