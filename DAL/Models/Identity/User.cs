using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.Identity {

	[Table("gm_user")]
	public class User : IdentityUser<long> {

		public User() : base() { }
		public User(string userName) : base(userName) {}

		// ---

		[Column("id")]
		public override long Id { get; set; }

		[Column("username"), MaxLength(256)]
		public override string UserName { get; set; }

		[Column("normalized_username"), MaxLength(256)]
		public override string NormalizedUserName { get; set; }

		[Column("email"), MaxLength(256)]
		public override string Email { get; set; }

		[Column("normalized_email"), MaxLength(256)]
		public override string NormalizedEmail { get; set; }

		[Column("phone_number"), MaxLength(64)]
		public override string PhoneNumber { get; set; }

		[Column("email_confirmed")]
		public override bool EmailConfirmed { get; set; }

		[Column("phone_number_confirmed")]
		public override bool PhoneNumberConfirmed { get; set; }

		[Column("password_hash"), MaxLength(512)]
		public override string PasswordHash { get; set; }

		[Column("lockout_end")]
		public override DateTimeOffset? LockoutEnd { get; set; }

		[Column("concurrency_stamp"), MaxLength(64)]
		public override string ConcurrencyStamp { get; set; }

		[Column("security_stamp"), MaxLength(64)]
		public override string SecurityStamp { get; set; }

		[Column("lockout_enabled")]
		public override bool LockoutEnabled { get; set; }

		[Column("access_failed_count")]
		public override int AccessFailedCount { get; set; }

		[Column("tfa_enabled")]
		public override bool TwoFactorEnabled { get; set; }

		// ---

		[Column("access_stamp_web"), MaxLength(64)]
		public string AccessStampWeb { get; set; }

		[Column("access_rights"), Required]
		public long AccessRights { get; set; }

		[Column("tfa_secret"), MaxLength(32), Required]
		public string TFASecret { get; set; }

		[Column("time_registered"), Required]
		public DateTime TimeRegistered { get; set; }

		// ---

		public virtual UserOptions UserOptions { get; set; }
		public virtual UserVerification UserVerification { get; set; }
		public virtual IEnumerable<KycShuftiProTicket> KycShuftiProTicket { get; set; }
		public virtual IEnumerable<Card> Card { get; set; }
		public virtual IEnumerable<CardPayment> CardPayment { get; set; }
		public virtual IEnumerable<SwiftPayment> SwiftPayment { get; set; }
		public virtual IEnumerable<Deposit> Deposit { get; set; }
		public virtual IEnumerable<Withdraw> Withdraw { get; set; }
		public virtual IEnumerable<UserActivity> UserActivity { get; set; }
		public virtual IEnumerable<BuyRequest> BuyRequest { get; set; }
		public virtual IEnumerable<SellRequest> SellRequest { get; set; }
		public virtual IEnumerable<FinancialHistory> FinancialHistory { get; set; }
		public virtual IEnumerable<UserOpLog> UserOpLog { get; set; }
	}
}
