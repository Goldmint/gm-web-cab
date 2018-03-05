namespace Goldmint.WebApplication.Core.Response {

	public enum APIErrorCode {

		/* [1..999] General errors, authorization */

		/// <summary>
		/// Just failure without any additional data
		/// </summary>
		InternalServerError = 1,

		/// <summary>
		/// Invalid request content type
		/// </summary>
		InvalidContentType = 2,

		/// <summary>
		/// Unauthorized request, have to sign in first
		/// </summary>
		Unauthorized = 50,

		/// <summary>
		/// Invalid request parameter
		/// </summary>
		InvalidParameter = 100,

		/// <summary>
		/// Ownership lost / data has not been modified / somebody else modified data first
		/// </summary>
		OwnershipLost = 101,

		/// <summary>
		/// Operation has rate limit; wait before next attempt
		/// </summary>
		RateLimit = 102,

		/* [1000..1999] Account errors */

		/// <summary>
		/// Account locked (automatic lockout)
		/// </summary>
		AccountNotFound = 1000,

		/// <summary>
		/// Account locked (automatic lockout)
		/// </summary>
		AccountLocked = 1001,

		/// <summary>
		/// Email not confirmed
		/// </summary>
		AccountEmailNotConfirmed = 1002,

		/// <summary>
		/// Account must be verified before action
		/// </summary>
		AccountNotVerified = 1003,

		/// <summary>
		/// Specified email is already taken
		/// </summary>
		AccountEmailTaken = 1004,

		/// <summary>
		/// Deposit limit reached
		/// </summary>
		AccountDepositLimit = 1005,

		/// <summary>
		/// Withdraw limit reached
		/// </summary>
		AccountWithdrawLimit = 1006,

		/// <summary>
		/// Card deposit failure
		/// </summary>
		AccountCardDepositFail = 1007,

		/// <summary>
		/// Card withdraw failure
		/// </summary>
		AccountCardWithdrawFail = 1008,

		/// <summary>
		/// TFA must be enabled
		/// </summary>
		AccountTFADisabled = 1009,

		/// <summary>
		/// User has pending operation. One of: buying, selling, deposit, withdraw, transfer, etc.
		/// </summary>
		AccountPendingBlockchainOperation = 1010,
		
		/// <summary>
		/// DPA is not signed
		/// </summary>
		AccountDpaNotSigned = 1011,
	}

	public static class APIErrorCodeExtensions {

		public static int ToIntCode(this APIErrorCode code) {
			return (int)code;
		}

		public static string ToDescription(this APIErrorCode code, string format = null, params object[] args) {
			return code.ToString() + (string.IsNullOrWhiteSpace(format) ? "" : ": " + string.Format(format, args));
		}
	}
}
