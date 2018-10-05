using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Goldmint.DAL.Models;

namespace Goldmint.DAL.ScannerModels {

	[Table("block")]
	public sealed class Block {

		[Column("id"), Key]
		public ulong Id { get; set; }

		[Column("number"), Required]
		public ulong Number { get; set; }

		//[Column("header_digest"), MaxLength(FieldMaxLength.SumusAddress), Required]
		//public string HeaderDigest { get; set; }

		//[Column("transaction_count"), Required]
		//public ulong TransactionCount { get; set; }

		//[Column("number_of_signers"), Required]
		//public uint NumberOfSigners { get; set; }

		//[Column("transferred_mnt"), Required]
		//public decimal TransferredMnt { get; set; }

		//[Column("transferred_gold"), Required]
		//public decimal TransferredGold { get; set; }

		//[Column("mnt_fee"), Required]
		//public decimal MntFee { get; set; }

		//[Column("gold_fee"), Required]
		//public decimal GoldFee { get; set; }

		//[Column("miner_node"), MaxLength(FieldMaxLength.SumusAddress), Required]
		//public string MinerNode { get; set; }

		//[Column(TypeName = "DateTime2")]
		//public DateTime TimeStamp { get; set; }

		//[Column("create_date"), Timestamp]
		//public DateTime CreateDate { get; set; }
	}
}
