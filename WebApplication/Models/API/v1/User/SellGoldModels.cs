using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.SellGoldModels {

	public class EstimateModel : BaseValidableModel {

		/// <summary>
		/// Ethereum address, optional
		/// </summary>
		public string EthAddress { get; set; }
		
		/// <summary>
		/// Fiat currency or cryptoasset 
		/// </summary>
		[Required]
		public string Currency { get; set; }

		/// <summary>
		/// Amount of GOLD (Reversed is false) or amount of Currency (Reversed is true)
		/// </summary>
		[Required]
		public string Amount { get; set; }

		/// <summary>
		/// False - GOLD to Currency estimation; true (reversed) - Currency to GOLD estimation
		/// </summary>
		[Required]
		public bool Reversed { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<EstimateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid format")
				.When(_ => !string.IsNullOrWhiteSpace(_.EthAddress))
				;

			v.RuleFor(_ => _.Currency)
				.NotEmpty().WithMessage("Invalid format")
				;

			v.RuleFor(_ => _.Amount)
				.NotEmpty().WithMessage("Invalid amount")
				;

			return v.Validate(this);
		}
	}

	public class EstimateView {

		/// <summary>
		/// Estimation amount. Currency amount minus fee (string or float, Reversed is false) or GOLD amount (string, Reversed is true)
		/// </summary>
		[Required]
		public object Amount { get; set; }

		/// <summary>
		/// Amount currency
		/// </summary>
		[Required]
		public string AmountCurrency { get; set; }

		/// <summary>
		/// Fee amount in Currency (string for cryptoasset, float for fiat)
		/// </summary>
		[Required]
		public object Fee { get; set; }

		/// <summary>
		/// Fee currency
		/// </summary>
		[Required]
		public string FeeCurrency { get; set; }
	}

	// ---

	public class ConfirmModel : BaseValidableModel {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long RequestId { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ConfirmModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.RequestId)
				.Must(Common.ValidationRules.BeValidId).WithMessage("Invalid id")
				;

			return v.Validate(this);
		}
	}

	public class ConfirmView {
	}

	// ---

	public class AssetEthModel : BaseValidableModel {

		/// <summary>
		/// Address
		/// </summary>
		[Required]
		public string EthAddress { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<AssetEthModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid format")
			;

			return v.Validate(this);
		}
	}

	public class AssetEthView {

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long RequestId { get; set; }

		/// <summary>
		/// Fiat currency
		/// </summary>
		[Required]
		public string Currency { get; set; }

		/// <summary>
		/// Amount per ETH
		/// </summary>
		[Required]
		public double EthRate { get; set; }

		/// <summary>
		/// Amount per GOLD
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

		/// <summary>
		/// ETH per GOLD
		/// </summary>
		[Required]
		public string EthPerGoldRate { get; set; }
		
		/// <summary>
		/// Expires at datetime (unixstamp)
		/// </summary>
		[Required]
		public long Expires { get; set; }
	}
}
