using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Goldmint.Common;
using Goldmint.DAL.Models.PromoCode;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard
{
    
    public class PromoCodesPagerView : BasePagerView<Pawn> {}

    public class UsedCodesPagerView : BasePagerView<UsedPromoCodes> {}

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
	    /// PromoCode valid for all GM users
	    /// </summary>
	    [Required]
	    public PromoCodeUsageType UsageType { get; set; }

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

        protected override FluentValidation.Results.ValidationResult ValidateFields()
        {
			var v = new InlineValidator<GenerateModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			return v.Validate(this);
		}
	}

    public class UsersInfo : BasePagerModel
    {
        /// <summary>
        /// PromoCode Id FK
        /// </summary>
        [Required]
        public long Id { get; set; }

        protected override FluentValidation.Results.ValidationResult ValidateFields()
        {
            var v = new InlineValidator<UsersInfo>() { CascadeMode = CascadeMode.StopOnFirstFailure };

            return v.Validate(this);
        }
    }


    public class GenerateView
	{

		/// <summary>
		/// Properties
		/// </summary>
		[Required]
		public string[] Codes { get; set; }
	}
}
