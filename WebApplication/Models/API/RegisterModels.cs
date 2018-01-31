using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.RegisterModels {

	public class RegisterModel : BaseValidableModel {

		/// <summary>
		/// Valid email /.{,256}/
		/// </summary>
		[Required]
		public string Email { get; set; }

		/// <summary>
		/// Password /.{6,128}/
		/// </summary>
		[Required]
		public string Password { get; set; }

		/// <summary>
		/// Captcha /.{1,1024}/
		/// </summary>
		[Required]
		public string Captcha { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<RegisterModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Email)
				.EmailAddress()
				.Must(Common.ValidationRules.BeValidEmailLength)
				.WithMessage("Invalid email format")
			;

			v.RuleFor(_ => _.Password)
				.Must(Common.ValidationRules.BeValidPassword)
				.WithMessage($"Password have to be from {Common.ValidationRules.PasswordMinLength} up to {Common.ValidationRules.PasswordMaxLength} symbols length")
			;

			v.RuleFor(_ => _.Captcha)
				.Must(Common.ValidationRules.BeValidCaptcha)
				.WithMessage("Invalid captcha token")
			;

			return v.Validate(this);
		}
	}

	// ---

	public class ConfirmModel : BaseValidableModel {

		/// <summary>
		/// Token
		/// </summary>
		[Required]
		public string Token { get; set; }
		
		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ConfirmModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Token)
				.Must(Common.ValidationRules.BeValidConfirmationToken)
				.WithMessage("Invalid token")
			;;

			return v.Validate(this);
		}
	}
}
