using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models {

	public abstract class BaseFinancialHistoryEntity : BaseUserEntity {

		[Column("ref_fin_history"), Required]
		public long RefFinancialHistoryId { get; set; }

		[ForeignKey(nameof(RefFinancialHistoryId))]
		public virtual FinancialHistory RefFinancialHistory { get; set; }

	}
}
