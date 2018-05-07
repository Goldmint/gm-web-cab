using FluentValidation;
using Goldmint.Common;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.GoldExchangeModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter by request ID, optional
		/// </summary>
		public long? FilterRequestId { get; set; }

		/// <summary>
		/// Selected period start (unixtime), optional
		/// </summary>
		public long? PeriodStart { get; set; }

		/// <summary>
		/// Selected period end (unixtime), optional
		/// </summary>
		public long? PeriodEnd { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.FilterRequestId)
				.GreaterThan(0).WithMessage("Invalid format")
				.When(_ => _.FilterRequestId != null)
				;

			v.RuleFor(_ => _.PeriodStart)
				.GreaterThan(0).WithMessage("Invalid format")
				.When(_ => _.PeriodStart != null)
				;

			v.RuleFor(_ => _.PeriodEnd)
				.GreaterThan(0).WithMessage("Invalid format")
				.When(_ => _.PeriodEnd != null)
				;

			v.RuleFor(_ => _.PeriodEnd)
				.GreaterThan(_ => _.PeriodStart).WithMessage("Invalid format")
				.When(_ => _.PeriodStart != null && _.PeriodEnd != null)
				;

			return v.Validate(this);
		}
	}
	
	public class ListView : BasePagerView<ListViewItem> {
	}

	public class ListViewItem {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long RequestId { get; set; }

		/// <summary>
		/// Is bought or sold
		/// </summary>
		[Required]
		public bool IsBuying { get; set; }

		/// <summary>
		/// Ethereum transaction ID
		/// </summary>
		public string EthTxId { get; set; }

		/// <summary>
		/// Amount in wei
		/// </summary>
		[Required]
		public string Amount { get; set; }

		/// <summary>
		/// User-initiator data
		/// </summary>
		[Required]
		public UserData User { get; set; }

		/// <summary>
		/// Date completed (unix)
		/// </summary>
		[Required]
		public long DateCompleted { get; set; }

		// ---

		public class UserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }
		}

		public class SupportUserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }

			/// <summary>
			/// Comment, optional
			/// </summary>
			public string Comment { get; set; }
		}
	}
}
