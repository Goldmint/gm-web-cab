using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Goldmint.Common;

namespace Goldmint.WebApplication.Models.API.v1.User.CryptoCapitalModels {

	public class DepositModel : BaseValidableModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<DepositModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };
			return v.Validate(this);
		}
	}

	public class DepositView {

		/// <summary>
		/// Company name
		/// </summary>
		[Required]
		public string CompanyName { get; set; }

		/// <summary>
		/// Address
		/// </summary>
		[Required]
		public string Address { get; set; }
		
		/// <summary>
		/// Country
		/// </summary>
		[Required]
		public string Country { get; set; }

		/// <summary>
		/// Beneficiary account
		/// </summary>
		[Required]
		public string BenAccount { get; set; }

		/// <summary>
		/// Reference
		/// </summary>
		[Required]
		public string Reference { get; set; }
	}

	// ---

	public class WithdrawModel : BaseValidableModel {

		/// <summary>
		/// Account ID
		/// </summary>
		[Required]
		public long AccountId { get; set; }

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

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<WithdrawModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.AccountId)
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
	}
}
