using FluentValidation;
using Goldmint.Common;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.SupportCryptoExchangeModels {

	public class ListBuyingModel : BasePagerModel {

		/// <summary>
		/// Exclude completed requests, optional
		/// </summary>
		public bool ExcludeCompleted { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListBuyingModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			return v.Validate(this);
		}
	}

	public class ListSellingModel : BasePagerModel {

		/// <summary>
		/// Exclude completed requests, optional
		/// </summary>
		public bool ExcludeCompleted { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListSellingModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			return v.Validate(this);
		}
	}

	public class ListView : BasePagerView<ListViewItem> {
	}

	public class ListViewItem {

		/// <summary>
		/// Internal ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Status: 1 - pending, 2 - success, 3 - cancelled
		/// </summary>
		[Required]
		public int Status { get; set; }

		/// <summary>
		/// Amount in wei
		/// </summary>
		[Required]
		public string Amount { get; set; }

		/// <summary>
		/// Exchange fiat currency
		/// </summary>
		[Required]
		public string ExchangeCurrency { get; set; }

		/// <summary>
		/// Exchange cryptoasset
		/// </summary>
		[Required]
		public string CryptoAsset { get; set; }

		/// <summary>
		/// User-initiator data
		/// </summary>
		[Required]
		public UserData User { get; set; }

		/// <summary>
		/// Support user info (completed request), optional
		/// </summary>
		public SupportUserData SupportUser { get; set; }

		/// <summary>
		/// Date created (unix)
		/// </summary>
		[Required]
		public long Date { get; set; }

		/// <summary>
		/// Date completed, optinal (unix)
		/// </summary>
		public long? DateCompleted { get; set; }

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

	// ---

	public class LockModel : BaseValidableModel {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<LockModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			return v.Validate(this);
		}
	}

	public class LockBuyingView {
		
		/// <summary>
		/// User-initiator data
		/// </summary>
		[Required]
		public UserData User { get; set; }

		// ---

		public class UserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }
		}
		
	}

	public class LockSellingView {
		
		/// <summary>
		/// User-initiator data
		/// </summary>
		[Required]
		public UserData User { get; set; }

		// ---

		public class UserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }
		}
	}

	/*
	// ---

	public class RefuseDepositModel : BaseValidableModel {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Support comment, requried
		/// </summary>
		[Required]
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RefuseDepositModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			v.RuleFor(_ => _.Comment)
				.NotEmpty().WithMessage("Empty")
				;

			return v.Validate(this);
		}
	}

	public class RefuseDepositView {
	}

	// ---

	public class RefuseWithdrawModel : BaseValidableModel {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Support comment, requried
		/// </summary>
		[Required]
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RefuseWithdrawModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			v.RuleFor(_ => _.Comment)
				.NotEmpty().WithMessage("Empty")
				;

			return v.Validate(this);
		}
	}

	public class RefuseWithdrawView {
	}

	// ---

	public class AcceptDepositModel : BaseValidableModel {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Support comment, optional
		/// </summary>
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AcceptDepositModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			v.RuleFor(_ => _.Amount)
				.GreaterThan(0).WithMessage("Invalid amount")
				;

			return v.Validate(this);

		}
	}

	public class AcceptDepositView {
	}
	*/
}
