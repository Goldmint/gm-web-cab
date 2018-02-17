using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.SettingsModels {

	public class TFAView {

		/// <summary>
		/// Is two factor auth enabled
		/// </summary>
		[Required]
		public bool Enabled { get; set; }

		/// <summary>
		/// QR code data if TFA is disabled or null
		/// </summary>
		public string QrData { get; set; }

		/// <summary>
		/// TFA secret if TFA is disabled or null
		/// </summary>
		public string Secret { get; set; }

	}

	public class TFAEditModel : BaseValidableModel {

		/// <summary>
		/// Code /[0-9]{6}/
		/// </summary>
		[Required]
		public string Code { get; set; }

		/// <summary>
		/// Enable/disable TFA
		/// </summary>
		[Required]
		public bool Enable { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<TFAEditModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidTFACode)
				.WithMessage("Invalid code format")
			;
			
			return v.Validate(this);
		}
	}

	// ---

	public class VerificationView : VerificationEditModel {

		/// <summary>
		/// Level 0 verification is completed (form filled)
		/// </summary>
		[Required]
		public bool HasVerificationL0 { get; set; }

		/// <summary>
		/// Level 1 verification is completed (kyc done)
		/// </summary>
		[Required]
		public bool HasVerificationL1 { get; set; }

	}

	public class VerificationEditModel : BaseValidableModel {

		/// <summary>
		/// First name /[a-zA-Z]{1,64}/
		/// </summary>
		[Required]
		public string FirstName { get; set; }

		/// <summary>
		/// Middle name or null
		/// </summary>
		public string MiddleName { get; set; }

		/// <summary>
		/// Last name /[a-zA-Z]{1,64}/
		/// </summary>
		[Required]
		public string LastName { get; set; }

		/// <summary>
		/// Day of birth /dd.mm.yyyy/
		/// </summary>
		[Required]
		public string Dob { get; set; }

		/// <summary>
		/// Phone number, international format: +xxxxx..x
		/// </summary>
		[Required]
		public string PhoneNumber { get; set; }

		/// <summary>
		/// Coutry: two-letter iso code: US, RU etc.
		/// </summary>
		[Required]
		public string Country { get; set; }

		/// <summary>
		/// State or province /[a-z0-9 -,.()]{1,256}/
		/// </summary>
		[Required]
		public string State { get; set; }

		/// <summary>
		/// City /[a-z0-9 -,.()]{1,256}/
		/// </summary>
		[Required]
		public string City { get; set; }

		/// <summary>
		/// Postal or zip /[0-9]{3,16}/
		/// </summary>
		[Required]
		public string PostalCode { get; set; }

		/// <summary>
		/// Street /[a-z0-9 -,.()]{1,256}/
		/// </summary>
		[Required]
		public string Street { get; set; }

		/// <summary>
		/// Apartment or null /[a-z0-9 -,.()]{1,128}/
		/// </summary>
		public string Apartment { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<VerificationEditModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.FirstName)
				.Must(Common.ValidationRules.BeValidName)
				.WithMessage("Invalid first name format")
			;

			v.RuleFor(_ => _.MiddleName)
				.Must(Common.ValidationRules.BeValidName)
				.WithMessage("Invalid middle name format")
				.When(_ => _.MiddleName != null && _.MiddleName != "")
			;

			v.RuleFor(_ => _.LastName)
				.Must(Common.ValidationRules.BeValidName)
				.WithMessage("Invalid last name format")
			;

			v.RuleFor(_ => _.Dob)
				.Must(Common.ValidationRules.BeValidDate)
				.WithMessage("Invalid date of birth format")
			;

			v.RuleFor(_ => _.PhoneNumber)
				.Must(Common.ValidationRules.BeValidPhone)
				.WithMessage("Invalid phone number format")
			;

			v.RuleFor(_ => _.Country)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2)
				.WithMessage("Invalid country format")
			;

			v.RuleFor(_ => _.State)
				.Must(Common.ValidationRules.ContainLatinAndPuncts)
				.Length(1, 256)
				.WithMessage("Invalid state format")
			;

			v.RuleFor(_ => _.City)
				.Must(Common.ValidationRules.ContainLatinAndPuncts)
				.Length(1, 256)
				.WithMessage("Invalid city format")
			;

			v.RuleFor(_ => _.PostalCode)
				.Must(Common.ValidationRules.ContainOnlyDigits)
				.Length(3, 16)
				.WithMessage("Invalid postal code format")
			;

			v.RuleFor(_ => _.Street)
				.Must(Common.ValidationRules.ContainLatinAndPuncts)
				.Length(1, 256)
				.WithMessage("Invalid street format")
			;

			v.RuleFor(_ => _.Apartment)
				.Must(Common.ValidationRules.ContainLatinAndPuncts)
				.Length(1, 128)
				.WithMessage("Invalid apartment format")
				.When(_ => _.Apartment != null && Apartment != "")
			;

			return v.Validate(this);
		}
	}

	// ---

	public class VerificationKycStartModel : BaseValidableModel {

		/// <summary>
		/// Redirect user to URL on KYC completion
		/// </summary>
		[Required]
		public string Redirect { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<VerificationKycStartModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Redirect)
				.Must(Common.ValidationRules.BeValidURL)
				.WithMessage("Invalid url format")
			;

			return v.Validate(this);
		}
	}

	public class VerificationKycStartView {

		/// <summary>
		/// Verification ticket ID to get status
		/// </summary>
		[Required]
		public string TicketId { get; set; }

		/// <summary>
		/// Redirect to KYC verifier
		/// </summary>
		[Required]
		public string Redirect { get; set; }

	}

	// ---

	public class VerificationKycStatusModel : BaseValidableModel {

		/// <summary>
		/// Ticket ID to track KYC status
		/// </summary>
		[Required]
		public string TicketId { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<VerificationKycStatusModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.TicketId)
				.Must(Common.ValidationRules.ContainOnlyDigits)
				.WithMessage("Invalid ticket id format")
			;

			return v.Validate(this);
		}
	}

	public class VerificationKycStatusView {

		/// <summary>
		/// True in case of verification is successful
		/// </summary>
		[Required]
		public bool Verified { get; set; }

	}

	// ---

	public class ChangePasswordModel : BaseValidableModel {

		/// <summary>
		/// Current password
		/// </summary>
		[Required]
		public string Current { get; set; }

		/// <summary>
		/// New password
		/// </summary>
		[Required]
		public string New { get; set; }

		/// <summary>
		/// Current core in case of 2fa enabled, optional
		/// </summary>
		public string TfaCode { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ChangePasswordModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.New)
				.Must(Common.ValidationRules.BeValidPassword)
				.WithMessage($"Password have to be from {Common.ValidationRules.PasswordMinLength} up to {Common.ValidationRules.PasswordMaxLength} symbols length")
				;

			return v.Validate(this);
		}
	}

	public class ChangePasswordView {
	}
}
