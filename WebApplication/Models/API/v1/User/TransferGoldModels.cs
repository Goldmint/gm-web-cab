using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.User.TransferGoldModels {

	public class HwTransferModel : BaseValidableModel {

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

		/// <summary>
		/// Two factor auth code /[0-9]{6}/, optional
		/// </summary>
		[Required]
		public string TfaCode { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<HwTransferModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid address")
				;

			v.RuleFor(_ => _.Amount)
				.NotEmpty().WithMessage("Invalid amount")
				;

			v.RuleFor(_ => _.TfaCode)
				.Must(Common.ValidationRules.BeValidTfaCode).WithMessage("Invalid format")
				.When(_ => _.TfaCode != null)
				;

			return v.Validate(this);
		}
	}

	public class HwTransferView {
	}
}
