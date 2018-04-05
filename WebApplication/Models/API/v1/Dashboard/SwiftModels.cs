using FluentValidation;
using Goldmint.Common;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.SwiftModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter query, optional, 64 max
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Exclude completed requests, optional
		/// </summary>
		public bool ExcludeCompleted { get; set; }

		/// <summary>
		/// Request type, optional: 0 - both (default), 1 - deposit, 2 - withdraw
		/// </summary>
		public int Type { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Filter)
				.MaximumLength(64).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Type)
				.InclusiveBetween(0, 2).WithMessage("Invalid format")
				;

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
		/// Request type: 1 - deposit, 2 - withdraw
		/// </summary>
		[Required]
		public int Type { get; set; }

		/// <summary>
		/// Status: 1 - pending, 2 - success, 3 - cancelled
		/// </summary>
		[Required]
		public int Status { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Payment reference
		/// </summary>
		[Required]
		public string PaymentReference { get; set; }

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

	public class LockDepositView {
		
		/// <summary>
		/// User-initiator data
		/// </summary>
		[Required]
		public UserData User { get; set; }

		/// <summary>
		/// Bank info
		/// </summary>
		[Required]
		public BankInfoData BankInfo { get; set; }

		// ---

		public class UserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }

			/// <summary>
			/// Fiat limits
			/// </summary>
			[Required]
			public PeriodLimitItem FiatLimits { get; set; }

			// ---

			public class PeriodLimitItem {

				/// <summary>
				/// Current limit (this-day-limit actually)
				/// </summary>
				[Required]
				public double Minimal { get; set; }

				/// <summary>
				/// This day limit
				/// </summary>
				[Required]
				public double Day { get; set; }

				/// <summary>
				/// This month limit
				/// </summary>
				[Required]
				public double Month { get; set; }
			}
		}

		public class BankInfoData {

			[Required]
			public string Name { get; set; }

			[Required]
			public string Address { get; set; }

			[Required]
			public string Iban { get; set; }

			[Required]
			public string BankName { get; set; }

			[Required]
			public string BankAddress { get; set; }

			[Required]
			public string Swift { get; set; }
		}
	}

	public class LockWithdrawView : LockDepositView {
	}

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

}
