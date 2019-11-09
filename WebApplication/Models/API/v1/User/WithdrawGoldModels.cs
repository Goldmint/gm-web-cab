using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.WithdrawGoldModels {

	public class LiteWalletModel : BaseValidableModel {

		/// <summary>
		/// Sumus address
		/// </summary>
		[Required]
		public string SumusAddress { get; set; }
		
		/// <summary>
		/// Amount of GOLD
		/// </summary>
		[Required]
		public decimal Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<LiteWalletModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.SumusAddress)
				.Must(Common.ValidationRules.BeValidSumusAddress).WithMessage("Invalid format")
				;

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(0.001m).WithMessage("Invalid amount")
				;

			return v.Validate(this);
		}
	}

	public class LiteWalletView {
	}
}
