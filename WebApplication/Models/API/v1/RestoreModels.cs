using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.RestoreModels {

	public class RestoreModel : BaseValidableModel {

		/// <summary>
		/// Valid email /.{,256}/
		/// </summary>
		[Required]
		public string Email { get; set; }

		/// <summary>
		/// Captcha /.{1,1024}/
		/// </summary>
		[Required]
		public string Captcha { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RestoreModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Email)
				.EmailAddress()
				.Must(Common.ValidationRules.BeValidEmailLength)
				.WithMessage("Invalid email format")
			;

			v.RuleFor(_ => _.Captcha)
				.Must(Common.ValidationRules.BeValidCaptcha)
				.WithMessage("Invalid captcha token")
			;

			return v.Validate(this);
		}
	}
	
	public class NewPasswordModel : BaseValidableModel {

		/// <summary>
		/// Confirmation token /.{1,512}/
		/// </summary>
		[Required]
		public string Token { get; set; }

		/// <summary>
		/// New password /.{6,128}/
		/// </summary>
		[Required]
		public string Password { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<NewPasswordModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Token)
				.Must(Common.ValidationRules.BeValidConfirmationToken)
				.WithMessage("Invalid token")
			;

			v.RuleFor(_ => _.Password)
				.Must(Common.ValidationRules.BeValidPassword)
				.WithMessage($"Password have to be from {Common.ValidationRules.PasswordMinLength} up to {Common.ValidationRules.PasswordMaxLength} symbols length")
			;

			return v.Validate(this);
		}
	}

}
