using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Models.API.CommonsModels {

	public class GoldRateView {

		/// <summary>
		/// USD amount per gold ounce
		/// </summary>
		[Required]
		public double Rate { get; set; }

	}
}
