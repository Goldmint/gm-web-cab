using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Goldmint.Common;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.PromoModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter query, optional
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Show only used
		/// </summary>
		public bool? FilterUsed { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };
			return v.Validate(this);
		}
	}

	public class ListView : BasePagerView<ListViewItem> {
	}

	public class ListViewItem {

		/// <summary>
		/// Internal ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Code
		/// </summary>
		[Required]
		public string Code { get; set; }
	    /// <summary>
	    /// Eth = 1,
	    /// Mnt = 2,
	    /// Gold = 3
	    /// </summary>
	    [Required]
	    public EthereumToken Currency { get; set; }

	    /// <summary>
	    /// Maximum tokens count
	    /// </summary>
	    [Required]
	    public decimal Limit { get; set; }

	    /// <summary>
	    /// Value of discount: (0, 100] %
	    /// </summary>
	    [Required]
	    public string DiscountValue { get; set; }

        /// <summary>
        /// Used by user or null
        /// </summary>
        [Required]
		public string Username { get; set; }

		/// <summary>
		/// Unixtime created
		/// </summary>
		[Required]
		public long TimeCreated { get; set; }

		/// <summary>
		/// Unixtime expires
		/// </summary>
		[Required]
		public long TimeExpires { get; set; }

		/// <summary>
		/// Unixtime used or null
		/// </summary>
		[Required]
		public long? TimeUsed { get; set; }
	}

	// ---

	public class GenerateModel : BaseValidableModel
	{

        /// <summary>
        /// Eth = 1,
        /// Mnt = 2,
        /// Gold = 3
        /// </summary>
        [Required]
	    public string Currency { get; set; }

	    /// <summary>
	    /// Maximum tokens count (^18)
	    /// </summary>
	    [Required]
	    public string Limit { get; set; }

        /// <summary>
        /// Value of discount (0-100000)
        /// </summary>
        [Required]
		public string DiscountValue { get; set; }

        /// <summary>
        /// Count to generate
        /// </summary>
        [Required]
	    public int Count { get; set; }

        /// <summary>
        /// Valid duration: (0, 500] days
        /// </summary>
        [Required]
		public double ValidForDays { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<GenerateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			return v.Validate(this);
		}
	}

	public class GenerateView {

		/// <summary>
		/// Properties
		/// </summary>
		[Required]
		public string[] Codes { get; set; }
	}
}
