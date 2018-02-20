using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.CountriesModels {

	public class BanModel : BaseValidableModel {
		
		/// <summary>
		/// Country code (alpha-2)
		/// </summary>
		[Required]
		public string Code { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<BanModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2)
				.WithMessage("Invalid country code")
			;

			v.RuleFor(_ => _.Comment)
				.NotNull()
				.MaximumLength(128)
				.WithMessage("Invalid comment")
			;

			return v.Validate(this);

		}
	}

	public class BanView {
	}

	// ---

	public class UnbanModel : BaseValidableModel {

		/// <summary>
		/// Country code (alpha-2)
		/// </summary>
		[Required]
		public string Code { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<UnbanModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2)
				.WithMessage("Invalid country code")
				;

			return v.Validate(this);

		}
	}

	public class UnbanView {
	}
}
