using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Goldmint.WebApplication.Models.API.v1.Migration
{
	public static class MigrationController
	{
		public class EthSumModel : BaseValidableModel
		{

			/// <summary>
			/// Sumus address
			/// </summary>
			[Required]
			public string SumusAddress { get; set; }

			/// <summary>
			/// Ethereum address
			/// </summary>
			[Required]
			public string EthereumAddress { get; set; }

			protected override FluentValidation.Results.ValidationResult ValidateFields()
			{
				var v = new InlineValidator<EthSumModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

				v.RuleFor(_ => _.SumusAddress)
					.Must(Common.ValidationRules.BeValidSumusAddress).WithMessage("Invalid format")
					;

				v.RuleFor(_ => _.EthereumAddress)
					.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid format")
					;

				return v.Validate(this);
			}
		}

		// ---

		public class SumEthModel : BaseValidableModel
		{

			/// <summary>
			/// Ethereum address
			/// </summary>
			[Required]
			public string EthereumAddress { get; set; }

			/// <summary>
			/// Sumus address
			/// </summary>
			[Required]
			public string SumusAddress { get; set; }

			protected override FluentValidation.Results.ValidationResult ValidateFields()
			{
				var v = new InlineValidator<SumEthModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

				v.RuleFor(_ => _.EthereumAddress)
					.Must(Common.ValidationRules.BeValidEthereumAddress).WithMessage("Invalid format")
					;

				v.RuleFor(_ => _.SumusAddress)
					.Must(Common.ValidationRules.BeValidSumusAddress).WithMessage("Invalid format")
					;

				return v.Validate(this);
			}
		}
	}
}
