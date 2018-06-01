using FluentValidation;
using Goldmint.Common;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.CreditCardModels {

	public class ListView {

		/// <summary>
		/// List
		/// </summary>
		[Required]
		public Item[] List { get; set; }

		// ---

		public class Item {

			/// <summary>
			/// Card ID
			/// </summary>
			[Required]
			public long CardId { get; set; }

			/// <summary>
			/// Card masked number
			/// </summary>
			[Required]
			public string Mask { get; set; }

			/// <summary>
			/// Card status: see 'status' method
			/// </summary>
			[Required]
			public string Status { get; set; }
		}
	}

	// ---

	public class AddModel : BaseValidableModel {

		/// <summary>
		/// URL to redirect user after form filling (`:cardId` will be replaced with card id)
		/// </summary>
		[Required]
		public string Redirect { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AddModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Redirect)
				.NotEmpty().WithMessage("Invalid format")
			;

			return v.Validate(this);

		}
	}

	public class AddView {

		/// <summary>
		/// New card ID
		/// </summary>
		[Required]
		public long CardId { get; set; }

		/// <summary>
		/// Redirect to acquirer gateway
		/// </summary>
		[Required]
		public string Redirect { get; set; }

	}

	// ---

	public class ConfirmModel : BaseValidableModel {

		/// <summary>
		/// Card ID
		/// </summary>
		[Required]
		public long CardId { get; set; }

		/// <summary>
		/// URL to redirect user after form filling (`:cardId` will be replaced with card id)
		/// </summary>
		[Required]
		public string Redirect { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ConfirmModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.CardId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
			;

			v.RuleFor(_ => _.Redirect)
				.NotEmpty().WithMessage("Invalid format")
			;

			return v.Validate(this);
		}
	}

	public class ConfirmView {

		/// <summary>
		/// Redirect to acquirer gateway
		/// </summary>
		[Required]
		public string Redirect { get; set; }

	}

	// ---

	public class VerifyModel : BaseValidableModel {

		/// <summary>
		/// Card ID
		/// </summary>
		[Required]
		public long CardId { get; set; }

		/// <summary>
		/// Card code from bank statement /.{1,10}/
		/// </summary>
		[Required]
		public string Code { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<VerifyModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.CardId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
			;

			v.RuleFor(_ => _.Code)
				.Length(1, 10).WithMessage("Invalid format")
			;

			return v.Validate(this);

		}
	}

	// ---

	public class StatusModel : BaseValidableModel {

		/// <summary>
		/// Card ID
		/// </summary>
		[Required]
		public long CardId { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<StatusModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.CardId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
			;

			return v.Validate(this);
		}
	}

	public class StatusView {

		/// <summary>
		/// Card status: initial, confirm, payment, verification, verified, disabled, failed
		/// </summary>
		[Required]
		public string Status { get; set; }
	}

	// ---

	public class RemoveModel : BaseValidableModel {

		/// <summary>
		/// Card ID
		/// </summary>
		[Required]
		public long CardId { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RemoveModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.CardId)
				.Must(ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			return v.Validate(this);

		}
	}

	public class RemoveView {
	}
}

