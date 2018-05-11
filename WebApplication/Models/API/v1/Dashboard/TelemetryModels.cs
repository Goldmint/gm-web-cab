using FluentValidation;
using FluentValidation.Results;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.TelemetryModels {

	public class TelemetryView {

		/// <summary>
		/// Telemetry Json data if available
		/// </summary>
		public string Aggregated { get; set; }
	}

	public class GetConfigView {

		/// <summary>
		/// Config Json data
		/// </summary>
		public string Config { get; set; }
	}

	public class SetConfigModel : BaseValidableModel {

		/// <summary>
		/// Config Json data
		/// </summary>
		public string Config { get; set; }

		protected override ValidationResult ValidateFields() {
			var v = new InlineValidator<SetConfigModel>() { CascadeMode = CascadeMode.StopOnFirstFailure };

			v.RuleFor(_ => _.Config)
				.NotEmpty().WithMessage("Invalid format")
				;

			return v.Validate(this);
		}
	}
}
