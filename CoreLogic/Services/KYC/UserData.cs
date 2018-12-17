using System;

namespace Goldmint.CoreLogic.Services.KYC {

	public class UserData {

		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string CountryCode { get; set; }
		public string LanguageCode { get; set; }
		public DateTime DoB { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
	}
}
