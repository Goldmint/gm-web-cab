using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API.v1.User.UserModels;
using Microsoft.AspNetCore.Mvc;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user")]
	public partial class UserController : BaseController {

		/// <summary>
		/// Zendesk SSO: new JWT token
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("zendesk/sso")]
		[ProducesResponseType(typeof(ZendeskSsoView), 200)]
		public async Task<APIResponse> ZendeskSso() {

			var user = await GetUserFromDb();
			var zdjwt = Core.Tokens.JWT.CreateZendeskSsoToken(AppConfig, user);
			return APIResponse.Success(
				new ZendeskSsoView() {
					Jwt = zdjwt,
				}
			);
		}

	}
}