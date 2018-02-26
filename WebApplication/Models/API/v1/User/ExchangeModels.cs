using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.ExchangeModels {

	public class BuyRequestModel : BaseValidableModel {

		/// <summary>
		/// Ethereum address
		/// </summary>
		[Required]
		public string EthAddress { get; set; }

		/// <summary>
		/// Fiat amount to exchange
		/// </summary>
		[Required]
		public double Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<BuyRequestModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress)
				.WithMessage("Invalid eth address format")
			;

			// any amount

			return v.Validate(this);
		}
	}

	public class BuyRequestView {

		/// <summary>
		/// Estimated gold amount in wei
		/// </summary>
		[Required]
		public string GoldAmount { get; set; }

		/// <summary>
		/// Gold rate used, now is fixed
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

		/// <summary>
		/// Payload to use in eth transaction
		/// </summary>
		[Required]
		public string[] Payload { get; set; }
	}

	// ---

	public class SellRequestModel : BaseValidableModel {

		/// <summary>
		/// Ethereum address
		/// </summary>
		[Required]
		public string EthAddress { get; set; }

		/// <summary>
		/// Gold amount in wei
		/// </summary>
		[Required]
		public string Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<SellRequestModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress)
				.WithMessage("Invalid eth address format")
			;

			v.RuleFor(_ => _.Amount)
				.NotEmpty()
				.WithMessage("Invalid amount")
			;

			return v.Validate(this);
		}
	}

	public class SellRequestView {

		/// <summary>
		/// Actual gold amount to burn in wei
		/// </summary>
		[Required]
		public string GoldAmount { get; set; }

		/// <summary>
		/// Estimated fiat amount
		/// </summary>
		[Required]
		public double FiatAmount { get; set; }

		/// <summary>
		/// Estimated fee amount
		/// </summary>
		[Required]
		public double FeeAmount { get; set; }

		/// <summary>
		/// Gold rate used, now is fixed
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

		/// <summary>
		/// Payload to use in eth transaction
		/// </summary>
		[Required]
		public string[] Payload { get; set; }
	}

	// ---

	public class HWBuyRequestModel : BaseValidableModel {

		/// <summary>
		/// Fiat amount to exchange
		/// </summary>
		[Required]
		public double Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<HWBuyRequestModel>();
			v.CascadeMode = CascadeMode.Continue;

			// any amount

			return v.Validate(this);
		}
	}

	public class HWBuyRequestView {

		/// <summary>
		/// Estimated gold amount in wei
		/// </summary>
		[Required]
		public string GoldAmount { get; set; }

		/// <summary>
		/// Gold rate used, now is fixed
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

		/// <summary>
		/// Request ID to confirm
		/// </summary>
		[Required]
		public long RequestId { get; set; }
	}

	// ---

	public class HWSellRequestModel : BaseValidableModel {

		/// <summary>
		/// Gold amount in wei
		/// </summary>
		[Required]
		public string Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<HWSellRequestModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.Amount)
				.NotEmpty()
				.WithMessage("Invalid amount")
				;

			return v.Validate(this);
		}
	}

	public class HWSellRequestView {

		/// <summary>
		/// Actual gold amount to burn in wei
		/// </summary>
		[Required]
		public string GoldAmount { get; set; }

		/// <summary>
		/// Estimated fiat amount
		/// </summary>
		[Required]
		public double FiatAmount { get; set; }

		/// <summary>
		/// Estimated fee amount
		/// </summary>
		[Required]
		public double FeeAmount { get; set; }

		/// <summary>
		/// Gold rate used, now is fixed
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

		/// <summary>
		/// Request ID to confirm
		/// </summary>
		[Required]
		public long RequestId { get; set; }
	}

	// ---

	public class HWConfirmModel : BaseValidableModel {

		/// <summary>
		/// Is it buying request ID?
		/// </summary>
		[Required]
		public bool IsBuying { get; set; }

		/// <summary>
		/// Buying/selling request ID
		/// </summary>
		[Required]
		public long RequestId { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<HWConfirmModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.RequestId)
				.Must(Common.ValidationRules.BeValidId)
				.WithMessage("Invalid request id")
			;

			return v.Validate(this);
		}
	}

	public class HWConfirmView {
	}
}
