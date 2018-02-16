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
			var v = new InlineValidator<TransparencyModel>();
			v.CascadeMode = CascadeMode.Continue;
			return v.Validate(this);
		}
	}

	public class TransparencyView : BasePagerView<TransparencyViewItem> {
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
}
