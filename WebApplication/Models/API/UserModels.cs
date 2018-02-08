using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Goldmint.WebApplication.Models.API.UserModels {

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

	public class BalanceModel : BaseValidableModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<BalanceModel>();
			v.CascadeMode = CascadeMode.Continue;

			return v.Validate(this);
		}
	}

	public class BalanceView {

		/// <summary>
		/// USD fiat amount
		/// </summary>
		[Required]
		public double Usd { get; set; }

		/// <summary>
		/// Gold token amount in wei
		/// </summary>
		[Required]
		public string Gold { get; set; }
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

		// ---

		public class UserLimits {

			public UserLimitItem Deposit { get; set; }
			public UserLimitItem Withdraw { get; set; }
		}

		public class VerificationLevelLimits {

			public LimitItem Deposit { get; set; }
			public LimitItem Withdraw { get; set; }
		}

		public class VerificationLevels {

			public VerificationLevelLimits L0 { get; set; }
			public VerificationLevelLimits L1 { get; set; }
		}

		public class LimitItem {

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

		public class UserLimitItem : LimitItem {

			/// <summary>
			/// Current limit
			/// </summary>
			[Required]
			public double Current { get; set; }
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
		/// Type: deposit, withdraw, etc
		/// </summary>
		[Required]
		public string Type { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		/// <summary>
		/// Amount data
		/// </summary>
		[Required]
		public AmountStruct Amount { get; set; }

		/// <summary>
		/// Fee data, optional
		/// </summary>
		public AmountStruct Fee { get; set; }

		/// <summary>
		/// Unixtime
		/// </summary>
		[Required]
		public long Date { get; set; }

		// ---

		public class AmountStruct {

			[Required]
			public double Amount { get; set; }

			/// <summary>
			/// Optional prefix
			/// </summary>
			public string Prefix { get; set; }
			
			/// <summary>
			/// Optional suffix
			/// </summary>
			public string Suffix { get; set; }

			public static AmountStruct Create(long cents, Common.FiatCurrency currency) {
				return new AmountStruct() {
					Amount = cents / 100d,
					Prefix = "",
					Suffix = " " + currency.ToString().ToUpper(),
				};
			}
		}
	}
}
