using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Goldmint.WebApplication.Models.API.v1.User.UserModels {

	public class AuthenticateModel : BaseValidableModel {

		/// <summary>
		/// Email or username /u[0-9]+/
		/// </summary>
		[Required]
		public string Username { get; set; }

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

		/// <summary>
		/// Valid audience or null
		/// </summary>
		public string Audience { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AuthenticateModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Username)
				.Must(Common.ValidationRules.BeValidUsername)
				.WithMessage("Invalid username format")
				.When(_ => !(_.Username?.Contains("@") ?? false))
			;

			v.RuleFor(_ => _.Username)
				.EmailAddress()
				.Must(Common.ValidationRules.BeValidEmailLength)
				.WithMessage("Invalid email format")
				.When(_ => (_.Username?.Contains("@") ?? false))
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

	public class AuthenticateView {

		/// <summary>
		/// Access token
		/// </summary>
		[Required]
		public string Token { get; set; }

		/// <summary>
		/// User have to complete 2 factor auth
		/// </summary>
		[Required]
		public bool TfaRequired { get; set; }
	}

	// ---

	public class RefreshView {

		/// <summary>
		/// Fresh access token
		/// </summary>
		[Required]
		public string Token { get; set; }
	}

	// ---

	public class TfaModel : BaseValidableModel {

		/// <summary>
		/// Two factor auth code /[0-9]{6}/
		/// </summary>
		[Required]
		public string Code { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<TfaModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Code)
				.Must(Common.ValidationRules.BeValidTFACode)
				.WithMessage("Invalid code format")
			;

			return v.Validate(this);
		}
	}

	// ---
	
	public class LimitsView {

		/// <summary>
		/// Current user fiat limits
		/// </summary>
		[Required]
		public UserLimits Current { get; set; }

		/// <summary>
		/// Limits by verification level 
		/// </summary>
		[Required]
		public VerificationLevels Levels { get; set; }

		/// <summary>
		/// Payment methods limits
		/// </summary>
		[Required]
		public PaymentMethods PaymentMethod { get; set; }

		// ---

		public class UserLimits {

			public UserPeriodLimitItem Deposit { get; set; }
			public UserPeriodLimitItem Withdraw { get; set; }
		}

		public class VerificationLevels {

			public VerificationLevelLimits Current { get; set; }
			public VerificationLevelLimits L0 { get; set; }
			public VerificationLevelLimits L1 { get; set; }
		}

		public class PaymentMethods {

			public PaymentMethodLimits Card { get; set; }
			public PaymentMethodLimits Swift { get; set; }
			public PaymentMethodLimits CryptoCapital { get; set; }
		}

		// ---

		public class VerificationLevelLimits {

			public PeriodLimitItem Deposit { get; set; }
			public PeriodLimitItem Withdraw { get; set; }
		}

		public class PaymentMethodLimits {

			public OnetimeLimitItem Deposit { get; set; }
			public OnetimeLimitItem Withdraw { get; set; }
		}

		// ---

		public class OnetimeLimitItem {

			/// <summary>
			/// Minimal value per operation
			/// </summary>
			[Required]
			public double Min { get; set; }
			
			/// <summary>
			/// Maximal value per operation
			/// </summary>
			[Required]
			public double Max { get; set; }
		}

		public class PeriodLimitItem {

			/// <summary>
			/// Day limit
			/// </summary>
			[Required]
			public double Day { get; set; }

			/// <summary>
			/// Month limit
			/// </summary>
			[Required]
			public double Month { get; set; }
		}

		public class UserPeriodLimitItem : PeriodLimitItem {

			/// <summary>
			/// Current limit
			/// </summary>
			[Required]
			public double Minimal { get; set; }
		}
	}

	// ---

	public class ProfileView {

		/// <summary>
		/// Id
		/// </summary>
		[Required]
		public string Id { get; set; }

		/// <summary>
		/// Fullname
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Email
		/// </summary>
		[Required]
		public string Email { get; set; }

		/// <summary>
		/// Has DPA signed
		/// </summary>
		[Required]
		public bool DpaSigned { get; set; }

		/// <summary>
		/// TFA enabled for this user
		/// </summary>
		[Required]
		public bool TfaEnabled { get; set; }

		/// <summary>
		/// Level 0 verification is completed
		/// </summary>
		[Required]
		public bool VerifiedL0 { get; set; }

		/// <summary>
		/// Level 1 verification is completed
		/// </summary>
		[Required]
		public bool VerifiedL1 { get; set; }

		/// <summary>
		/// User challenges to pass through
		/// </summary>
		[Required]
		public string[] Challenges { get; set; }
	}

	// ---

	public class ActivityModel : BasePagerModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ActivityModel>();
			v.CascadeMode = CascadeMode.Continue;
			return v.Validate(this);
		}
	}

	public class ActivityView : BasePagerView<ActivityViewItem> {
	}

	public class ActivityViewItem {

		/// <summary>
		/// Type of activity
		/// </summary>
		[Required]
		public string Type { get; set; }

		/// <summary>
		/// Activity comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		/// <summary>
		/// Client IP
		/// </summary>
		[Required]
		public string Ip { get; set; }

		/// <summary>
		/// Client agent
		/// </summary>
		[Required]
		public string Agent { get; set; }

		/// <summary>
		/// Unixtime
		/// </summary>
		[Required]
		public long Date { get; set; } 
	}

	// ---

	public class FiatHistoryModel : BasePagerModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<FiatHistoryModel>();
			v.CascadeMode = CascadeMode.Continue;
			return v.Validate(this);
		}
	}

	public class FiatHistoryView : BasePagerView<FiatHistoryViewItem> {
	}

	public class FiatHistoryViewItem {

		/// <summary>
		/// Type: deposit, withdraw, goldbuy, goldsell, hwtransfer
		/// </summary>
		[Required]
		public string Type { get; set; }
		
		/// <summary>
		/// Status: 1 - pending, 2 - successful, 3 - cancelled
		/// </summary>
		[Required]
		public int Status { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }
		
		/// <summary>
		/// Ethereum transaction ID to track, optional
		/// </summary>
		public string EthTxId { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Fee, optional
		/// </summary>
		public double? Fee { get; set; }

		/// <summary>
		/// Unixtime
		/// </summary>
		[Required]
		public long Date { get; set; }
	}

	// ---

	public class ZendeskSsoView {

		/// <summary>
		/// JWT to use as Zendesk-SSO payload
		/// </summary>
		[Required]
		public string Jwt { get; set; }
	}

}
