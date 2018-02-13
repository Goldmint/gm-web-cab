using Goldmint.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using FluentValidation;

namespace Goldmint.WebApplication.Models.API.SwiftModels {
	
	public class DepositModel : BaseValidableModel {

		/// <summary>
		/// USD amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<DepositModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(1)
				.WithMessage("Invalid amount")
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

		/// <summary>
		/// Prepared html to show
		/// </summary>
		[Required]
		public string Html { get; set; }
	}

	// ---

	public class WithdrawModel : BaseValidableModel {

		/// <summary>
		/// USD amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

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

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<WithdrawModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(1)
				.WithMessage("Invalid amount")
			;

			v.RuleFor(_ => _.BenName)
				.NotEmpty()
				.MaximumLength(256)
				.WithMessage("Invalid name")
			;

			v.RuleFor(_ => _.BenAddress)
				.NotEmpty()
				.MaximumLength(512)
				.WithMessage("Invalid address")
			;

			v.RuleFor(_ => _.BenIban)
				.NotEmpty()
				.MaximumLength(128)
				.WithMessage("Invalid iban")
			;

			v.RuleFor(_ => _.BenBankName)
				.NotEmpty()
				.MaximumLength(256)
				.WithMessage("Invalid bank name")
			;

			v.RuleFor(_ => _.BenBankAddress)
				.NotEmpty()
				.MaximumLength(512)
				.WithMessage("Invalid bank address")
			;

			v.RuleFor(_ => _.BenSwift)
				.NotEmpty()
				.MaximumLength(64)
				.WithMessage("Invalid swift")
			;

			return v.Validate(this);

		}
	}

	public class WithdrawView {

		[Required]
		public string Reference { get; set; }
	}
}
