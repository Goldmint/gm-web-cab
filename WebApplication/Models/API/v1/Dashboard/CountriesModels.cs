using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.CountriesModels {

	public class ListModel : BasePagerModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>();
			v.CascadeMode = CascadeMode.Continue;
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
		/// Comment /.{0,128}/
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
		/// Country code (alpha-2): US, RU...
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
