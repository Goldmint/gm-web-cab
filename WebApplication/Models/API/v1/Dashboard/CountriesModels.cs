using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.CountriesModels {

	public class ListModel : BasePagerModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };
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
		/// Country code
		/// </summary>
		[Required]
		public string Code { get; set; }

		/// <summary>
		/// Country name
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		/// <summary>
		/// Date created (unix)
		/// </summary>
		[Required]
		public long Date { get; set; }
	}

	// ---

	public class BanModel : BaseValidableModel {
		
		/// <summary>
		/// Country code (alpha-2): US, RU...
		/// </summary>
		[Required]
		public string Code { get; set; }

		/// <summary>
		/// Comment /.{0,128}/, optional
		/// </summary>
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<BanModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2).WithMessage("Invalid format")
			;

			v.RuleFor(_ => _.Comment)
				.MaximumLength(128).WithMessage("Invalid length")
			;

			return v.Validate(this);

		}
	}

	public class BanView {
	}

	// ---

	public class UnbanModel : BaseValidableModel {

		/// <summary>
		/// Country code (alpha-2): US, RU...
		/// </summary>
		[Required]
		public string Code { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<UnbanModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2).WithMessage("Invalid format")
				;

			return v.Validate(this);

		}
	}

	public class UnbanView {
	}
}
