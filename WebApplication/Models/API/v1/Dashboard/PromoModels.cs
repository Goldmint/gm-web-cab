using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Goldmint.Common;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.PromoModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter query, optional
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Show only used
		/// </summary>
		public bool? FilterUsed { get; set; }

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
		/// Code
		/// </summary>
		[Required]
		public string Code { get; set; }

		/// <summary>
		/// Value
		/// </summary>
		[Required]
		public string Value { get; set; }
		
		/// <summary>
		/// Used by user or null
		/// </summary>
		[Required]
		public string Username { get; set; }

		/// <summary>
		/// Unixtime created
		/// </summary>
		[Required]
		public long TimeCreated { get; set; }

		/// <summary>
		/// Unixtime expires
		/// </summary>
		[Required]
		public long TimeExpires { get; set; }

		/// <summary>
		/// Unixtime used or null
		/// </summary>
		[Required]
		public long? TimeUsed { get; set; }
	}

	// ---

	public class GenerateModel : BaseValidableModel {

		/// <summary>
		/// Count to generate
		/// </summary>
		[Required]
		public long Count { get; set; }

		/// <summary>
		/// Value of discount: from 0 to 0.999
		/// </summary>
		[Required]
		public double Value { get; set; }

		/// <summary>
		/// Valid duration
		/// </summary>
		[Required]
		public long ValidForDays { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<GenerateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Count)
				.GreaterThan(0).LessThanOrEqualTo(500).WithMessage("Invalid format")
				;

			v.RuleFor(_ => _.Value)
				.GreaterThanOrEqualTo(0).LessThanOrEqualTo(0.999).WithMessage("Invalid format")
				;

			v.RuleFor(_ => _.ValidForDays)
				.GreaterThan(0).WithMessage("Invalid format")
				;

			return v.Validate(this);
		}
	}

	public class GenerateView {

		/// <summary>
		/// Properties
		/// </summary>
		[Required]
		public string[] Codes { get; set; }
	}
}
