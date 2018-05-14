using System.Linq;
using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.Dashboard.TelemetryModels;
using Goldmint.WebApplication.Services.Bus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Goldmint.CoreLogic.Services.Bus.Proto;

namespace Goldmint.WebApplication.Controllers.v1.Dashboard {

	[Route("api/v1/dashboard/telemetry")]
	public class TelemetryController : BaseController {

		/// <summary>
		/// Telemetry
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.DashboardReadAccess)]
		[HttpGet, Route("telemetry")]
		[ProducesResponseType(typeof(TelemetryView), 200)]
		public APIResponse Telemetry() {

			var aggregated = "{}";

			var ath = HttpContext.RequestServices.GetService<AggregatedTelemetryHolder>();
			if (ath != null) {
				aggregated = ath.GetJson();
			}

			return APIResponse.Success(
				new TelemetryView() {
					Aggregated = aggregated,
				}
			);
		}

		/// <summary>
		/// Get runtime config
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Owner)]
		[HttpGet, Route("getConfig")]
		[ProducesResponseType(typeof(GetConfigView), 200)]
		public APIResponse GetConfig() {
			var rcfg = RuntimeConfigHolder.Clone();
			return APIResponse.Success(
				new GetConfigView() {
					Config = Common.Json.Stringify(rcfg),
				}
			);
		}

		/// <summary>
		/// Update runtime config
		/// </summary>
		[RequireJWTAudience(JwtAudience.Dashboard), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Owner)]
		[HttpPost, Route("setConfig")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> SetConfig([FromBody] SetConfigModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// parse into config
			var newConfig = RuntimeConfigHolder.Clone();
			if (!Common.Json.ParseInto(model.Config, newConfig)) {
				return APIResponse.BadRequest(nameof(model.Config), "Invalid format");
			}

			// validate
			var validation = RuntimeConfig.GetValidator().Validate(newConfig);
			if (!validation.IsValid) {
				return APIResponse.BadRequest(nameof(model.Config), validation.Errors?.FirstOrDefault()?.ToString() ?? "");
			}

			// try to save then publish
			var rcfg = Common.Json.Stringify(newConfig);
			var cl = HttpContext.RequestServices.GetService<IRuntimeConfigLoader>();
			if (cl != null && await cl.Save(rcfg)) {

				// broadcast
				var busPub = HttpContext.RequestServices.GetService<CoreLogic.Services.Bus.Publisher.ChildPublisher>();
				if (busPub != null) {
					busPub.PublishMessage(
						Topic.ConfigUpdated,
						new object()
					);
				}

				return APIResponse.Success();
			}

			return APIResponse.GeneralInternalFailure();
		}
	}
}
