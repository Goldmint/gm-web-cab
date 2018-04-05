using FluentValidation;
using Goldmint.Common;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.CryptoCapitalModels {

	public class SetupDepositModel : BaseValidableModel {

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

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<SetupDepositModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.CompanyName)
				.NotEmpty().WithMessage("Empty")
				;
			v.RuleFor(_ => _.Address)
				.NotEmpty().WithMessage("Empty")
				;
			v.RuleFor(_ => _.Country)
				.NotEmpty().WithMessage("Empty")
				;
			v.RuleFor(_ => _.BenAccount)
				.NotEmpty().WithMessage("Empty")
				;
			
			return v.Validate(this);
		}
	}

	public class SetupDepositView {
	}
}
