using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.CryptoExchangeModels {

	public class EthDepositModel : BaseValidableModel {

		/// <summary>
		/// Address
		/// </summary>
		[Required]
		public string EthAddress { get; set; }

		/// <summary>
		/// Amount of ETH in wei
		/// </summary>
		[Required]
		public string Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<EthDepositModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid format")
			;

			v.RuleFor(_ => _.Amount)
				.NotEmpty().WithMessage("Invalid amount")
			;

			return v.Validate(this);
		}
	}

	public class EthDepositView {

		/// <summary>
		/// Amount per eth
		/// </summary>
		[Required]
		public double EthRate { get; set; }

		/// <summary>
		/// Request ID
		/// </summary>
		[Required]
		public long RequestId { get; set; }
		
		/// <summary>
		/// Confirmed request will expire in .. (seconds)
		/// </summary>
		[Required]
		public long ExpiresIn { get; set; }
	}

	// ---

	public class ConfirmModel : BaseValidableModel {

		/// <summary>
		/// Is it deposit request ID?
		/// </summary>
		[Required]
		public bool IsDeposit { get; set; }

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
}
