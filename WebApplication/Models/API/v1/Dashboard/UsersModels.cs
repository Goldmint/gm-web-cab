using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Goldmint.Common;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.UsersModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter query, optional
		/// </summary>
		public string Filter { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };
			return v.Validate(this);
		}
	}

	public class ListView : BasePagerView<ListViewItem> {
	}

	public class ListViewItem {

		/// <summary>
		/// User internal ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Username
		/// </summary>
		[Required]
		public string Username { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Unixtime registered
		/// </summary>
		[Required]
		public long TimeRegistered { get; set; }
	}

	// ---

	public class AccountModel : BaseValidableModel {

		/// <summary>
		/// User ID
		/// </summary>
		[Required]
		public long Id { get; set; }
		
		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AccountModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;
			
			return v.Validate(this);

		}
	}

	public class AccountView {

		/// <summary>
		/// Properties
		/// </summary>
		[Required]
		public PropertiesItem[] Properties { get; set; }

		/// <summary>
		/// Access rights
		/// </summary>
		[Required]
		public AccessRightsItem[] AccessRights { get; set; }

		// ---

		public class PropertiesItem {

			/// <summary>
			/// Name
			/// </summary>
			public string N { get; set; }

			/// <summary>
			/// Value
			/// </summary>
			public string V { get; set; }
		}

		public class AccessRightsItem {

			/// <summary>
			/// Name
			/// </summary>
			public string N { get; set; }

			/// <summary>
			/// Checked
			/// </summary>
			public bool C { get; set; }

			/// <summary>
			/// Mask
			/// </summary>
			public long M { get; set; }
		}
	}

	// ---

	public class OplogModel : BasePagerModel {

		/// <summary>
		/// User ID
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Filter query, optional
		/// </summary>
		public string Filter { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<OplogModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			return v.Validate(this);
		}
	}

	public class OplogView : BasePagerView<OplogViewItem> {
	}

	public class OplogViewItem {

		/// <summary>
		/// ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Message
		/// </summary>
		[Required]
		public string Message { get; set; }

		/// <summary>
		/// Status, see enums
		/// </summary>
		[Required]
		public int Status { get; set; }

		/// <summary>
		/// Unixtime
		/// </summary>
		[Required]
		public long Date { get; set; }

		/// <summary>
		/// Sub-items, optional
		/// </summary>
		public OplogViewItem[] Steps { get; set; }

	}

	// ---

	public class RightsModel : BaseValidableModel {

		/// <summary>
		/// User ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// New mask
		/// </summary>
		[Required]
		public long Mask { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RightsModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			v.RuleFor(_ => _.Mask)
				.GreaterThanOrEqualTo(0).WithMessage("Invalid mask")
				.LessThan(((long)AccessRights.Owner) << 1).WithMessage("Invalid mask")
				;

			return v.Validate(this);

		}
	}

	public class RightsView {
	}

	// ---

	public class ProveResidenceModel : BaseValidableModel {

		/// <summary>
		/// User ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Proved
		/// </summary>
		[Required]
		public bool Proved { get; set; }

		/// <summary>
		/// Link, /.{1,512}/ or null if unproved
		/// </summary>
		[Required]
		public string Link { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ProveResidenceModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Id)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			v.RuleFor(_ => _.Link)
				.Must(ValidationRules.BeValidUrl).WithMessage("Invalid format")
				.MaximumLength(DAL.Models.FieldMaxLength.Comment).WithMessage("Invalid length")
				.When(_ => _.Proved)
				;

			return v.Validate(this);

		}
	}

	public class ProveResidenceView {
	}
}
