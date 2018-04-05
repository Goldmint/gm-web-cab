using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.FiatFeesModels {
	
	public class UpdateModel : BaseValidableModel {

		/// <summary>
		/// Fiat currencies
		/// </summary>
		[Required]
		public CommonsModels.FeesViewCurrency[] Fiat { get; set; }

		/// <summary>
		/// Cryptoassets
		/// </summary>
		[Required]
		public CommonsModels.FeesViewCurrency[] Crypto { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<UpdateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Fiat)
				.NotEmpty().WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Fiat)
				.Must(_ => !string.IsNullOrWhiteSpace(_.Name)).WithMessage("Empty")
				.Must(_ => _.Methods != null && _.Methods.Length > 0).WithMessage("Methods count")
				.Must(_ => {
					foreach (var m in _.Methods) {
						if (string.IsNullOrWhiteSpace(m.Name)) return false;
						if (string.IsNullOrWhiteSpace(m.Deposit)) return false;
						if (string.IsNullOrWhiteSpace(m.Withdraw)) return false;
					}
					return true;
				}).WithMessage("Method name/data")
				;

			v.RuleFor(_ => _.Crypto)
				.NotEmpty().WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Crypto)
				.Must(_ => !string.IsNullOrWhiteSpace(_.Name)).WithMessage("Empty")
				.Must(_ => _.Methods != null && _.Methods.Length > 0).WithMessage("Methods count")
				.Must(_ => {
					foreach (var m in _.Methods) {
						if (string.IsNullOrWhiteSpace(m.Name)) return false;
						if (string.IsNullOrWhiteSpace(m.Deposit)) return false;
						if (string.IsNullOrWhiteSpace(m.Withdraw)) return false;
					}
					return true;
				}).WithMessage("Method name/data")
				;

			return v.Validate(this);
		}
	}

	public class UpdateView {
	}
}
