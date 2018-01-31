using Goldmint.Common;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Core.Policies {

	public class RequireAreaToken : IAuthorizationRequirement {

		private JwtArea _area;

		public RequireAreaToken(JwtArea area) {
			_area = area;
		}

		public class Handler : AuthorizationHandler<RequireAreaToken> {

			protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireAreaToken requirement) {

				if (context.User.HasClaim(Tokens.JWT.GMAreaField, requirement._area.ToString().ToLower())) {
					context.Succeed(requirement);
				}
				return Task.CompletedTask;
			}
		}
	}
}
