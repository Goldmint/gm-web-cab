using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.TransparencyModels {

	public class AddModel : BaseValidableModel {
		
		/// <summary>
		/// Fiat amount
		/// </summary>
		[Required]
		public string Amount { get; set; }

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
				.NotNull().WithMessage("Invalid amount")
				.MaximumLength(DAL.Models.FieldMaxLength.Comment).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Hash)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(DAL.Models.FieldMaxLength.TransparencyTransactionHash).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Comment)
				.NotEmpty().WithMessage("Empty")
				.MaximumLength(DAL.Models.FieldMaxLength.Comment).WithMessage("Invalid length")
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
		/// Total oz field
		/// </summary>
		[Required]
		public string TotalOz { get; set; }

		/// <summary>
		/// Total USD field
		/// </summary>
		[Required]
		public string TotalUsd { get; set; }

		/// <summary>
		/// Data timestamp (unix, optional)
		/// </summary>
		public long? DataTimestamp { get; set; }

		/// <summary>
		/// Audit timestamp (unix, optional)
		/// </summary>
		public long? AuditTimestamp { get; set; }

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

			v.RuleFor(_ => _.TotalOz)
				.NotEmpty().WithMessage("Empty")
				;

			v.RuleFor(_ => _.TotalUsd)
				.NotEmpty().WithMessage("Empty")
				;

			v.RuleFor(_ => _.DataTimestamp)
				.GreaterThan(0).WithMessage("Invalid timestamp")
				.When(_ => _.DataTimestamp != null)
				;

			v.RuleFor(_ => _.AuditTimestamp)
				.GreaterThan(0).WithMessage("Invalid timestamp")
				.When(_ => _.AuditTimestamp != null)
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
