using Goldmint.Common;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace Goldmint.WebApplication.Core.Policies {

	public class RequireAccessRights : IAuthorizationRequirement {

		private AccessRights _rights;

		public RequireAccessRights(AccessRights rights) {
			_rights = rights;
		}

		public class Handler : AuthorizationHandler<RequireAccessRights> {

			protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireAccessRights requirement) {

				var rightsStr = context.User.Claims.Where(c => c.Type == Tokens.JWT.GMRightsField).FirstOrDefault()?.Value ?? "0";

				long rights = 0;
				if (long.TryParse(rightsStr, out rights)) {
					var req = (long)requirement._rights;
					if ((rights & req) == req) {
						context.Succeed(requirement);
					}
				}

				return Task.CompletedTask;
			}
		}
	}
}
