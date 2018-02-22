using Goldmint.Common;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Core.Policies {

	public class RequireJWTArea : IAuthorizationRequirement {

		private JwtArea _area;

		public RequireJWTArea(JwtArea area) {
			_area = area;
		}

		public class Handler : AuthorizationHandler<RequireJWTArea> {

			protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireJWTArea requirement) {

				if (context.User.HasClaim(Tokens.JWT.GMAreaField, requirement._area.ToString().ToLower())) {
					context.Succeed(requirement);
					return Task.CompletedTask;
				}

				context.Fail();
				return Task.CompletedTask;
			}
		}
	}
}
