using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.TransparencyModels {

	public class AddModel : BaseValidableModel {
		
		/// <summary>
		/// Fiat amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Document hash (IPFS hash)
		/// </summary>
		[Required]
		public string Hash { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AddModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(0.01)
				.WithMessage("Invalid amount")
			;

			v.RuleFor(_ => _.Hash)
				.NotEmpty()
				.MaximumLength(128)
				.WithMessage("Invalid hash")
			;

			v.RuleFor(_ => _.Comment)
				.NotNull()
				.MaximumLength(512)
				.WithMessage("Invalid comment")
			;

			return v.Validate(this);

		}
	}

	public class AddView {
	}
}
