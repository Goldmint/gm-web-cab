using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Goldmint.DAL.Models;

namespace Goldmint.DAL.ScannerModels {

	[Table("transaction")]
	public sealed class Transaction {
		[Column("id"), Key]
		public ulong Id { get; set; }

		[Column("unique_id"), Required]
		public string UniqueId { get; set; }

		[Column("block_number"), Required]
		public ulong BlockNumber { get; set; }

		[Column("tx_id"), Required]
		public ulong TransactionId { get; set; }

		[Column("token_type"), MaxLength(15), Required]
		public string TokenType { get; set; }

		[Column("tokens_count"), Required]
		public decimal TokensCount { get; set; }

		[Column("tx_fee"), Required]
		public decimal TransactionFee { get; set; }

		[Column("source_wallet"), MaxLength(FieldMaxLength.SumusTransactionHash), Required]
		public string SourceWallet { get; set; }

		[Column("destination_wallet"), MaxLength(FieldMaxLength.SumusTransactionHash), Required]
		public string DestinationWallet { get; set; }

		[Column("timestamp", TypeName = "DateTime2")]
		public DateTime TimeStamp { get; set; }

		[Column("create_date"), Timestamp]
		public DateTime CreateDate { get; set; }
	}

}
