using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard.SwiftModels {

	public class ListModel : BasePagerModel {

		/// <summary>
		/// Filter query, optional
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Exclude completed requests, optional
		/// </summary>
		public bool ExcludeCompleted { get; set; }

		protected override FluentValidation.Results.ValidationResult ValidateFields() {
			var v = new InlineValidator<ListModel>();
			v.CascadeMode = CascadeMode.Continue;
			return v.Validate(this);
		}
	}

	public class ListView : BasePagerView<ListViewItem> {
	}

	public class ListViewItem {

		/// <summary>
		/// Internal ID
		/// </summary>
		[Required]
		public long Id { get; set; }

		/// <summary>
		/// Request type: 1 - deposit, 2 - withdraw
		/// </summary>
		[Required]
		public int Type { get; set; }

		/// <summary>
		/// Status: 1 - pending, 2 - success, 3 - cancelled
		/// </summary>
		[Required]
		public int Status { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Required]
		public double Amount { get; set; }

		/// <summary>
		/// Payment reference
		/// </summary>
		[Required]
		public string PaymentReference { get; set; }

		/// <summary>
		/// User (initiator) info
		/// </summary>
		[Required]
		public UserData User { get; set; }

		/// <summary>
		/// Support user info (completed request), optional
		/// </summary>
		public SupportUserData SupportUser { get; set; }

		/// <summary>
		/// Date created (unix)
		/// </summary>
		[Required]
		public long Date { get; set; }

		/// <summary>
		/// Date completed, optinal (unix)
		/// </summary>
		public long? DateCompleted { get; set; }

		// ---

		public class UserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }
		}

		public class SupportUserData {

			/// <summary>
			/// Username (u000000)
			/// </summary>
			[Required]
			public string Username { get; set; }

			/// <summary>
			/// Comment, optional
			/// </summary>
			public string Comment { get; set; }
		}

		public class BankInfoData {

			[Required]
			public string Name { get; set; }

			[Required]
			public string Address { get; set; }

			[Required]
			public string Iban { get; set; }

			[Required]
			public string BankName { get; set; }

			[Required]
			public string BankAddress { get; set; }

			[Required]
			public string Swift { get; set; }

			[Required]
			public string Country { get; set; }
		}
	}
}
