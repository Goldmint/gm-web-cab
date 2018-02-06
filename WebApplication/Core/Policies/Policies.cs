using Microsoft.AspNetCore.Authorization;
using System;

namespace Goldmint.WebApplication.Core.Policies {

	public static class Policy {

		/// <summary>
		/// Has access to 2fa area
		/// </summary>
		public const string AccessTFAArea = "PolicyAreaTfa";

		/// <summary>
		/// Has access to authorized area
		/// </summary>
		public const string AccessAuthorizedArea = "PolicyAreaAuthorized";

		/// <summary>
		/// Has access rights
		/// </summary>
		public const string HasAccessRightsTemplate = "PolicyAccessRights_";
	}

	// ---

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class AreaAnonymousAttribute : AllowAnonymousAttribute {
		public AreaAnonymousAttribute() : base() { }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class AreaAuthorizedAttribute : AuthorizeAttribute {
		public AreaAuthorizedAttribute() : base(Policies.Policy.AccessAuthorizedArea) { }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class AreaTFAAttribute : AuthorizeAttribute {
		public AreaTFAAttribute() : base(Policies.Policy.AccessTFAArea) { }
	}

	// ---

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class AccessRightsAttribute : AuthorizeAttribute {
		public AccessRightsAttribute(Common.AccessRights rights) : base(Policies.Policy.HasAccessRightsTemplate + rights.ToString()) { }
	}
}
