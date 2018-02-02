using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Models.API.ExchangeModels {

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
		/// Estimated gold amount
		/// </summary>
		[Required]
		public double GoldAmount { get; set; }

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
		/// Gold amount to sell
		/// </summary>
		[Required]
		public double Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<SellRequestModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress)
				.WithMessage("Invalid eth address format")
			;

			// any amount

			return v.Validate(this);
		}
	}

	public class SellRequestView {

		/// <summary>
		/// Estimated fiat amount
		/// </summary>
		[Required]
		public double FiatAmount { get; set; }

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

	public class BuyRequestDryModel : BaseValidableModel {

		/// <summary>
		/// Ethereum address, optional
		/// </summary>
		public string EthAddress { get; set; }

		/// <summary>
		/// Fiat amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<BuyRequestDryModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress)
				.When(_ => _.EthAddress != null)
				.WithMessage("Invalid eth address format")
			;

			// any amount

			return v.Validate(this);
		}
	}

	public class BuyRequestDryView {
		
		/// <summary>
		/// Estimated gold amount
		/// </summary>
		[Required]
		public double GoldAmount { get; set; }

		/// <summary>
		/// Fiat amount used in estimation
		/// </summary>
		[Required]
		public double AmountUsed { get; set; }

		/// <summary>
		/// Minimum fiat amount
		/// </summary>
		[Required]
		public double AmountMin { get; set; }
		
		/// <summary>
		/// Maximum fiat amount
		/// </summary>
		[Required]
		public double AmountMax { get; set; }

		/// <summary>
		/// Gold rate used
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

	}

	// ---

	public class SellRequestDryModel : BaseValidableModel {

		/// <summary>
		/// Ethereum address, optional
		/// </summary>
		public string EthAddress { get; set; }

		/// <summary>
		/// Gold amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		// ---

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<SellRequestDryModel>();
			v.CascadeMode = CascadeMode.Continue;

			v.RuleFor(_ => _.EthAddress)
				.Must(Common.ValidationRules.BeValidEthereumAddress)
				.When(_ => _.EthAddress != null)
				.WithMessage("Invalid eth address format")
			;

			// any amount

			return v.Validate(this);
		}
	}

	public class SellRequestDryView {
		
		/// <summary>
		/// Estimated fiat amount
		/// </summary>
		[Required]
		public double FiatAmount { get; set; }

		/// <summary>
		/// Gold amount used in estimation
		/// </summary>
		[Required]
		public double AmountUsed { get; set; }

		/// <summary>
		/// Minimum gold amount
		/// </summary>
		[Required]
		public double AmountMin { get; set; }

		/// <summary>
		/// Maximum gold amount
		/// </summary>
		[Required]
		public double AmountMax { get; set; }

		/// <summary>
		/// Gold rate used
		/// </summary>
		[Required]
		public double GoldRate { get; set; }

	}

	
}
