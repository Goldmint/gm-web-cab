using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.SettingsModels {

	public class DpaStatusView {

		/// <summary>
		/// DPA is signed
		/// </summary>
		[Required]
		public bool IsSigned { get; set; }

		/// <summary>
		/// DPA could be resent
		/// </summary>
		[Required]
		public bool CanResend { get; set; }
	}

	// ---
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
			var v = new InlineValidator<TFAEditModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidTFACode).WithMessage("Invalid format")
			;
			
			return v.Validate(this);
		}
	}

	// ---

	public class VerificationView : VerificationEditModel {
		
		/// <summary>
		/// Form is filled
		/// </summary>
		[Required]
		public bool IsFormFilled { get; set; }
		
		/// <summary>
		/// KYC flow is pending
		/// </summary>
		[Required]
		public bool IsKycPending { get; set; }

		/// <summary>
		/// KYC flow completed
		/// </summary>
		[Required]
		public bool IsKycFinished { get; set; }

		/// <summary>
		/// Agreement signature is pending
		/// </summary>
		[Required]
		public bool IsAgreementPending { get; set; }
		
		/// <summary>
		/// Agreement is signed
		/// </summary>
		[Required]
		public bool IsAgreementSigned { get; set; }
	}

	public class VerificationEditModel : BaseValidableModel {

		/// <summary>
		/// First name /[a-zA-Z]{2,32}/
		/// </summary>
		[Required]
		public string FirstName { get; set; }

		/// <summary>
		/// Middle name /[a-zA-Z]{2,32}/ or null
		/// </summary>
		public string MiddleName { get; set; }

		/// <summary>
		/// Last name /[a-zA-Z]{2,32}/
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
			var v = new InlineValidator<VerificationEditModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.FirstName)
				.Must(Common.ValidationRules.BeValidName).WithMessage("Invalid format")
			;

			v.RuleFor(_ => _.MiddleName)
				.Must(Common.ValidationRules.BeValidName).WithMessage("Invalid format")
				.When(_ => !string.IsNullOrEmpty(_.MiddleName))
			;

			v.RuleFor(_ => _.LastName)
				.Must(Common.ValidationRules.BeValidName).WithMessage("Invalid format")
			;

			v.RuleFor(_ => _.Dob)
				.Must(Common.ValidationRules.BeValidDate).WithMessage("Invalid format")
			;

			v.RuleFor(_ => _.PhoneNumber)
				.Must(Common.ValidationRules.BeValidPhone).WithMessage("Invalid phone number format")
			;

			v.RuleFor(_ => _.Country)
				.Must(Common.ValidationRules.BeValidCountryCodeAlpha2).WithMessage("Invalid country format")
			;

			v.RuleFor(_ => _.State)
				.Must(Common.ValidationRules.ContainLatinAndPuncts).WithMessage("Invalid format")
				.Length(1, 256).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.City)
				.Must(Common.ValidationRules.ContainLatinAndPuncts).WithMessage("Invalid format")
				.Length(1, 256).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.PostalCode)
				.Must(Common.ValidationRules.ContainOnlyDigits).WithMessage("Invalid format")
				.Length(3, 16).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Street)
				.Must(Common.ValidationRules.ContainLatinAndPuncts).WithMessage("Invalid format")
				.Length(1, 256).WithMessage("Invalid length")
			;

			v.RuleFor(_ => _.Apartment)
				.Must(Common.ValidationRules.ContainLatinAndPuncts).WithMessage("Invalid format")
				.Length(1, 128).WithMessage("Invalid length")
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
			var v = new InlineValidator<VerificationKycStartModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Redirect)
				.Must(Common.ValidationRules.BeValidURL).WithMessage("Invalid format")
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
			var v = new InlineValidator<ChangePasswordModel>() {CascadeMode = CascadeMode.StopOnFirstFailure};

			v.RuleFor(_ => _.New)
				.Must(Common.ValidationRules.BeValidPasswordLength).WithMessage("Invalid length")
				;

			return v.Validate(this);
		}
	}

	public class ChangePasswordView {
	}
}
