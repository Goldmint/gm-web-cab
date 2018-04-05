using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Goldmint.Common;

namespace Goldmint.WebApplication.Models.API.v1.User.SwiftModels {

	public class ListView {

		/// <summary>
		/// List
		/// </summary>
		[Required]
		public Item[] List { get; set; }

		// ---

		public class Item {

			/// <summary>
			/// Template ID
			/// </summary>
			[Required]
			public long TemplateId { get; set; }

			/// <summary>
			/// Template name
			/// </summary>
			[Required]
			public string Name { get; set; }

			/// <summary>
			/// Account holder's name
			/// </summary>
			[Required]
			public string Holder { get; set; }

			/// <summary>
			/// IBAN, account number
			/// </summary>
			[Required]
			public string Iban { get; set; }

			/// <summary>
			/// Bank name
			/// </summary>
			[Required]
			public string Bank { get; set; }

			/// <summary>
			/// BIC / SWIFT
			/// </summary>
			[Required]
			public string Bic { get; set; }

			/// <summary>
			/// Details
			/// </summary>
			[Required]
			public string Details { get; set; }
		}
	}

	// ---

	public class AddModel : BaseValidableModel {

		/// <summary>
		/// Template name {1,64}
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Account holder's name, .{1,256}
		/// </summary>
		[Required]
		public string Holder { get; set; }

		/// <summary>
		/// IBAN, account number, .{1,256}
		/// </summary>
		[Required]
		public string Iban { get; set; }

		/// <summary>
		/// Bank name, .{1,256}
		/// </summary>
		[Required]
		public string Bank { get; set; }

		/// <summary>
		/// BIC / SWIFT, .{1,128}
		/// </summary>
		[Required]
		public string Bic { get; set; }

		/// <summary>
		/// Details, .{1-1024}
		/// </summary>
		[Required]
		public string Details { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AddModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Name)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(64).WithMessage("Invalid length")
				;

			v.RuleFor(_ => _.Holder)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(256).WithMessage("Invalid length")
				;

			v.RuleFor(_ => _.Iban)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(256).WithMessage("Invalid length")
				;

			v.RuleFor(_ => _.Bank)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(256).WithMessage("Invalid length")
				;

			v.RuleFor(_ => _.Bic)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(128).WithMessage("Invalid length")
				;

			v.RuleFor(_ => _.Details)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(1024).WithMessage("Invalid length")
				;

			return v.Validate(this);

		}
	}

	public class AddView {
	}

	// ---

	public class RemoveModel : BaseValidableModel {

		/// <summary>
		/// Template ID
		/// </summary>
		[Required]
		public long TemplateId { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RemoveModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.TemplateId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			return v.Validate(this);

		}
	}

	public class RemoveView {
	}

	// ---

	public class DepositModel : BaseValidableModel {

		/// <summary>
		/// USD amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<DepositModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(1).WithMessage("Invalid amount")
			;

			return v.Validate(this);
		}
	}

	public class DepositView {

		/// <summary>
		/// Beneficiary name
		/// </summary>
		[Required]
		public string BenName { get; set; }

		/// <summary>
		/// Beneficiary address
		/// </summary>
		[Required]
		public string BenAddress { get; set; }

		/// <summary>
		/// Beneficiary IBAN
		/// </summary>
		[Required]
		public string BenIban { get; set; }

		/// <summary>
		/// Beneficiary bank name
		/// </summary>
		[Required]
		public string BenBankName { get; set; }

		/// <summary>
		/// Beneficiary bank addr
		/// </summary>
		[Required]
		public string BenBankAddress { get; set; }

		/// <summary>
		/// Beneficiary bank SWIFT
		/// </summary>
		[Required]
		public string BenSwift { get; set; }

		/// <summary>
		/// Payment ref
		/// </summary>
		[Required]
		public string Reference { get; set; }
	}

	// ---

	public class WithdrawModel : BaseValidableModel {

		/// <summary>
		/// Saved swift template ID
		/// </summary>
		[Required]
		public long TemplateId { get; set; }
		
		/// <summary>
		/// USD amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// TFA Code /[0-9]{6}/
		/// </summary>
		[Required]
		public string Code { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<WithdrawModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.TemplateId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
			;

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(1).WithMessage("Invalid amount")
			;

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidTFACode).WithMessage("Invalid format")
				;

			return v.Validate(this);

		}
	}

	public class WithdrawView {

		[Required]
		public string Reference { get; set; }
	}
}
