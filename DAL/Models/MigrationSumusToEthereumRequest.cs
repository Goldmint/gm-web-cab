using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;

namespace Goldmint.DAL.Models
{

	[Table("gm_se_token_migration")]
	public class MigrationSumusToEthereumRequest : BaseEntity, IConcurrentUpdate
	{

		[Column("asset"), Required]
		public MigrationRequestAsset Asset { get; set; }

		[Column("status"), Required]
		public MigrationRequestStatus Status { get; set; }

		[Column("sum_address"), MaxLength(FieldMaxLength.SumusAddress), Required]
		public string SumAddress { get; set; }

		[Column("eth_address"), MaxLength(FieldMaxLength.EthereumAddress), Required]
		public string EthAddress { get; set; }

		[Column("amount")]
		public decimal? Amount { get; set; }

		[Column("block")]
		public ulong? Block { get; set; }

		[Column("sum_txid"), MaxLength(FieldMaxLength.SumusTransactionHash)]
		public string SumTransaction { get; set; }

		[Column("eth_txid"), MaxLength(FieldMaxLength.EthereumTransactionHash)]
		public string EthTransaction { get; set; }

		[Column("time_created"), Required]
		public DateTime TimeCreated { get; set; }

		[Column("time_next_check"), Required]
		public DateTime TimeNextCheck { get; set; }

		[Column("time_completed")]
		public DateTime? TimeCompleted { get; set; }

		[Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen() {
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
