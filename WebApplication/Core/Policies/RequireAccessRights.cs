﻿using Goldmint.Common;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Goldmint.WebApplication.Core.Tokens;

namespace Goldmint.WebApplication.Core.Policies {

	public class RequireAccessRights : IAuthorizationRequirement {

		private AccessRights _rights;

		public RequireAccessRights(AccessRights rights) {
			_rights = rights;
		}

		public class Handler : AuthorizationHandler<RequireAccessRights> {

			protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireAccessRights requirement) {

				var rightsStr = context.User.Claims.FirstOrDefault(c => c.Type == JWT.GMRightsField)?.Value ?? "0";

				if (long.TryParse(rightsStr, out var rights)) {
					var req = (long)requirement._rights;
					if ((rights & req) == req) {
						context.Succeed(requirement);
						return Task.CompletedTask;
					}
				}

				context.Fail();
				return Task.CompletedTask;
			}
		}
	}
}
