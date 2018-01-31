﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.Identity {

	public class RoleClaim : IdentityRoleClaim<long> {

		public RoleClaim() : base() { }

		// ---

		[Column("id")]
		public override int Id { get; set; }

		[Column("role_id")]
		public override long RoleId { get; set; }

		[Column("claim_type"), MaxLength(256)]
		public override string ClaimType { get; set; }

		[Column("claim_value"), MaxLength(256)]
		public override string ClaimValue { get; set; }
	}
}
