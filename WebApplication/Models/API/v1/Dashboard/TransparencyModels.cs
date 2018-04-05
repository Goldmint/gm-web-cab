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
			var v = new InlineValidator<AddModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Amount)
				.GreaterThanOrEqualTo(0.01).WithMessage("Invalid amount")
			;

			v.RuleFor(_ => _.Hash)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(128).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Comment)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(512).WithMessage("Invalid length")
			;

			return v.Validate(this);

		}
	}

	public class AddView {
	}

	// ---

	public class UpdateStatModel : BaseValidableModel {

		/// <summary>
		/// Assets array
		/// </summary>
		[Required]
		public Item[] Assets { get; set; }

		/// <summary>
		/// Bonds array
		/// </summary>
		[Required]
		public Item[] Bonds { get; set; }

		/// <summary>
		/// Fiat array
		/// </summary>
		[Required]
		public Item[] Fiat { get; set; }

		/// <summary>
		/// Gold array
		/// </summary>
		[Required]
		public Item[] Gold { get; set; }

		/// <summary>
		/// Data timestamp (unix)
		/// </summary>
		[Required]
		public long DataTimestamp { get; set; }

		/// <summary>
		/// Audit timestamp (unix)
		/// </summary>
		[Required]
		public long AuditTimestamp { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<UpdateStatModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Assets)
				.NotEmpty().WithMessage("Empty")
				.Must(_ => Common.Json.Stringify(_).Length <= DAL.Models.TransparencyStat.MaxJsonFieldLength).WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Assets)
				.Must(_ => !string.IsNullOrWhiteSpace(_.K) && !string.IsNullOrWhiteSpace(_.V)).WithMessage("Empty")
				;

			v.RuleFor(_ => _.Bonds)
				.NotEmpty().WithMessage("Empty")
				.Must(_ => Common.Json.Stringify(_).Length <= DAL.Models.TransparencyStat.MaxJsonFieldLength).WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Bonds)
				.Must(_ => !string.IsNullOrWhiteSpace(_.K) && !string.IsNullOrWhiteSpace(_.V)).WithMessage("Empty")
				;

			v.RuleFor(_ => _.Fiat)
				.NotEmpty().WithMessage("Empty")
				.Must(_ => Common.Json.Stringify(_).Length <= DAL.Models.TransparencyStat.MaxJsonFieldLength).WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Fiat)
				.Must(_ => !string.IsNullOrWhiteSpace(_.K) && !string.IsNullOrWhiteSpace(_.V)).WithMessage("Empty")
				;

			v.RuleFor(_ => _.Gold)
				.NotEmpty().WithMessage("Empty")
				.Must(_ => Common.Json.Stringify(_).Length <= DAL.Models.TransparencyStat.MaxJsonFieldLength).WithMessage("Invalid length")
				;
			v.RuleForEach(_ => _.Gold)
				.Must(_ => !string.IsNullOrWhiteSpace(_.K) && !string.IsNullOrWhiteSpace(_.V)).WithMessage("Empty")
				;

			v.RuleFor(_ => _.DataTimestamp)
				.GreaterThan(0).WithMessage("Invalid timestamp")
				;

			v.RuleFor(_ => _.AuditTimestamp)
				.GreaterThan(0).WithMessage("Invalid timestamp")
				;

			return v.Validate(this);

		}

		public class Item {

			[Required]
			public string K { get; set; }

			[Required]
			public string V { get; set; }
		}
	}

	public class UpdateStatView {
	}
}
