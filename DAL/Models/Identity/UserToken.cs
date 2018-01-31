﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.Identity {

	public class UserToken : IdentityUserToken<long> {

		public UserToken() : base() { }

		// ---

		[Column("user_id")]
		public override long UserId { get; set; }

		[Column("login_provider"), MaxLength(64)]
		public override string LoginProvider { get; set; }

		[Column("name"), MaxLength(128)]
		public override string Name { get; set; }

		[Column("value"), MaxLength(128)]
		public override string Value { get; set; }
	}
}
