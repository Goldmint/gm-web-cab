using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Models.API.v1.CommonsModels {

	public class GoldRateView {

		/// <summary>
		/// USD amount per gold ounce
		/// </summary>
		[Required]
		public double Rate { get; set; }
	}

	// ---

	public class TransparencyModel : BasePagerModel {

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<TransparencyModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			return v.Validate(this);
		}
	}

	public class TransparencyView : BasePagerView<TransparencyViewItem> {

		/// <summary>
		/// Current transparency stat
		/// </summary>
		[Required]
		public TransparencyViewStat Stat { get; set; }

	}

	public class TransparencyViewItem {

		/// <summary>
		/// Amount in USD
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Link to document
		/// </summary>
		[Required]
		public string Link { get; set; }

		/// <summary>
		/// Comment
		/// </summary>
		[Required]
		public string Comment { get; set; }

		/// <summary>
		/// Unixtime
		/// </summary>
		[Required]
		public long Date { get; set; }
	}

	public class TransparencyViewStat {

		/// <summary>
		/// Assets array
		/// </summary>
		[Required]
		public TransparencyViewStatItem[] Assets { get; set; }

		/// <summary>
		/// Liabilities array
		/// </summary>
		[Required]
		public TransparencyViewStatItem[] Bonds { get; set; }

		/// <summary>
		/// Fiat data array
		/// </summary>
		[Required]
		public TransparencyViewStatItem[] Fiat { get; set; }

		/// <summary>
		/// Physical gold data array
		/// </summary>
		[Required]
		public TransparencyViewStatItem[] Gold { get; set; }

		/// <summary>
		/// Data provided time (unix)
		/// </summary>
		[Required]
		public long DataTimestamp { get; set; }

		/// <summary>
		/// Audit time (unix)
		/// </summary>
		[Required]
		public long AuditTimestamp { get; set; }
	}

	public class TransparencyViewStatItem {

		/// <summary>
		/// Item key
		/// </summary>
		[Required]
		public string K { get; set; }

		/// <summary>
		/// Item value
		/// </summary>
		[Required]
		public string V { get; set; }
	}

	// ---

	public class FeesView {

		/// <summary>
		/// Fiat currencies
		/// </summary>
		[Required]
		public FeesViewCurrency[] Fiat { get; set; }

		/// <summary>
		/// Cryptoassets
		/// </summary>
		[Required]
		public FeesViewCurrency[] Crypto { get; set; }
	}

	public class FeesViewCurrency {

		/// <summary>
		/// Currency name: USD, EUR etc.
		/// </summary>
		[Required]
		public string Name { get; set; }
		
		/// <summary>
		/// Currency methods
		/// </summary>
		[Required]
		public FeesViewMethod[] Methods { get; set; }
	}

	public class FeesViewMethod {

		/// <summary>
		/// Method name: VISA, MC etc.
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Deposit data
		/// </summary>
		[Required]
		public string Deposit { get; set; }

		/// <summary>
		/// Withdraw data
		/// </summary>
		[Required]
		public string Withdraw { get; set; }
	}
}
