using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels {

	public class EstimateModel : BaseValidableModel {

		/// <summary>
		/// Fiat currency or cryptoasset 
		/// </summary>
		[Required]
		public string Currency { get; set; }

		/// <summary>
		/// Amount of Currency (Reversed is false) or amount of GOLD (Reversed is true)
		/// </summary>
		[Required]
		public string Amount { get; set; }

		/// <summary>
		/// False - Currency to GOLD estimation; true (reversed) - GOLD to Currency estimation
		/// </summary>
		[Required]
		public bool Reversed { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<EstimateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

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
		/// Estimation amount. GOLD amount (string, Reversed is false) or Currency amount (string or float, Reversed is true)
		/// </summary>
		[Required]
		public object Amount { get; set; }

		/// <summary>
		/// Amount currency
		/// </summary>
		[Required]
		public string AmountCurrency { get; set; }
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
		/// Cents per ETH
		/// </summary>
		[Required]
		public double EthRate { get; set; }

		/// <summary>
		/// Cents per GOLD
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
