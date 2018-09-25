using FluentValidation;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard
{
    public class NoInputPagerModel : BasePagerModel
    {
        protected override FluentValidation.Results.ValidationResult ValidateFields()
        {
            var v = new InlineValidator<NoInputPagerModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

            return v.Validate(this);
        }
    }

}