using System;

namespace Goldmint.Common {

	#region Auth

	public enum Locale {

		En = 1,
		Ru,
	}

	public enum LoginProvider {

		Google = 1,
	}

	public enum JwtAudience {

		/// <summary>
		/// App access
		/// </summary>
		Cabinet = 1,

		/// <summary>
		/// Dashboard access
		/// </summary>
		Dashboard,
	}

	public enum JwtArea {

		/// <summary>
		/// Authorized area
		/// </summary>
		Authorized = 1,

		/// <summary>
		/// TFA area
		/// </summary>
		Tfa,

		/// <summary>
		/// OAuth area
		/// </summary>
		OAuth,

		/// <summary>
		/// User registration
		/// </summary>
		Registration,

		/// <summary>
		/// User password restoration
		/// </summary>
		RestorePassword,

		/// <summary>
		/// DPA awaiting area
		/// </summary>
		Dpa,
	}

	public enum AccessRights : long {

		/// <summary>
		/// App client
		/// </summary>
		Client = 0x1L,

		/// <summary>
		/// Dashboard: general access, read access
		/// </summary>
		DashboardReadAccess = 0x2L,

		/// <summary>
		/// App client - extra access
		/// </summary>
		ClientExtraAccess = 0x4L,

		// ---

		/// <summary>
		/// Dashboard: buy requests write access
		/// </summary>
		BuyRequestsWriteAccess = 0x2000000L,

		/// <summary>
		/// Dashboard: sell requests write access
		/// </summary>
		SellRequestsWriteAccess = 0x4000000L,

		/// <summary>
		/// Dashboard: promo codes access
		/// </summary>
		PromoCodesWriteAccess = 0x8000000L,

		/// <summary>
		/// Dashboard: user list write access
		/// </summary>
		UsersWriteAccess = 0x10000000L,

		/// <summary>
		/// Dashboard: countries tab write access
		/// </summary>
		CountriesWriteAccess = 0x20000000L,

		/// <summary>
		/// Dashboard: transparency tab write access
		/// </summary>
		TransparencyWriteAccess = 0x40000000L,

		/// <summary>
		/// Critical functions access
		/// </summary>
		Owner = 0x80000000L,
	}

	public enum UserTier {

		Tier0 = 0,
		Tier1 = 1,
		Tier2 = 2,
	}

	#endregion

	#region Sell GOLD

	public enum SellGoldRequestStatus {

		/// <summary>
		/// Just created
		/// </summary>
		Unconfirmed = 1,

		/// <summary>
		/// Confirmed by user
		/// </summary>
		Confirmed,

		/// <summary>
		/// Final success
		/// </summary>
		Success,

		/// <summary>
		/// Final failure
		/// </summary>
		Failed,
	}

	#endregion

	#region Ethereum blockchain

	public enum EthereumToken {

		Eth = 1,
		Mnt = 2,
		Gold = 3
	}

	public enum EthereumOperationStatus {

		/// <summary>
		/// Enqueued
		/// </summary>
		Initial = 1,

		/// <summary>
		/// Sending request to blockchain
		/// </summary>
		BlockchainInit,

		/// <summary>
		/// Waiting confirmation from blockchain
		/// </summary>
		BlockchainConfirm,

		/// <summary>
		/// Success
		/// </summary>
		Success,

		/// <summary>
		/// Failed
		/// </summary>
		Failed,
	}

	public enum EthTransactionStatus {

		/// <summary>
		/// Unconfirmed status, still outside of any block
		/// </summary>
		Pending = 1,

		/// <summary>
		/// Transaction confirmed
		/// </summary>
		Success,

		/// <summary>
		/// Transaction cancelled or failed
		/// </summary>
		Failed,
	}

	#endregion

	#region Sumus blockchain

	public enum SumusToken {

		/// <summary>
		/// MNT token
		/// </summary>
		Mnt = 1,

		/// <summary>
		/// GOLD token
		/// </summary>
		Gold = 2,
	}

	public enum EmissionRequestStatus : int {

		/// <summary>
		/// Enqueued
		/// </summary>
		Initial = 1,

		/// <summary>
		/// Request passed to emission service
		/// </summary>
		Requested,

		/// <summary>
		/// Done
		/// </summary>
		Completed,

		/// <summary>
		/// Failure
		/// </summary>
		Failed,
	}

	#endregion

	#region Credit Card / Bank Account

	public enum CardState {

		/// <summary>
		/// User should provide card data for deposit operations
		/// </summary>
		InputDepositData = 1,

		/// <summary>
		/// User should provide card data for withdraw operations
		/// </summary>
		InputWithdrawData,

		/// <summary>
		/// Sending verification payment
		/// </summary>
		Payment,

		/// <summary>
		/// Waiting for code from bank statement
		/// </summary>
		Verification,

		/// <summary>
		/// Verified
		/// </summary>
		Verified,

		/// <summary>
		/// Disabled by system
		/// </summary>
		Disabled,

		/// <summary>
		/// Deleted
		/// </summary>
		Deleted,
	}

	public enum CardPaymentType {

		/// <summary>
		/// Card verification
		/// </summary>
		Verification = 1,

		/// <summary>
		/// User's deposit payment
		/// </summary>
		Deposit,

		/// <summary>
		/// Card deposit refund on limit
		/// </summary>
		Refund,

		/// <summary>
		/// Withdraw operation
		/// </summary>
		Withdraw,

		/// <summary>
		/// Not payment actually but needed to control card addition
		/// </summary>
		CardDataInputSMS = 10,
		CardDataInputCRD,
		CardDataInputP2P,
	}

	public enum CardGatewayTransactionStatus {

		/// <summary>
		/// Processing
		/// </summary>
		Pending = 1,

		/// <summary>
		/// Finally successful
		/// </summary>
		Success,

		/// <summary>
		/// Completely failed
		/// </summary>
		Failed,

		/// <summary>
		/// Not found on GW side
		/// </summary>
		NotFound
	}

	public enum CardPaymentStatus {

		/// <summary>
		/// Unconfirmed initial state, just enqueued
		/// </summary>
		Unconfirmed = 1,

		/// <summary>
		/// Initial state, waiting for processing
		/// </summary>
		Pending,

		/// <summary>
		/// There is an attempt to charge
		/// </summary>
		Charging,

		/// <summary>
		/// Final success, finalized
		/// </summary>
		Success,

		/// <summary>
		/// Final failure, finalized
		/// </summary>
		Failed,
	}

	#endregion

	#region User

	public enum UserOpLogStatus {

		/// <summary>
		/// Operation is pending
		/// </summary>
		Pending = 1,

		/// <summary>
		/// Operation succesfully completed
		/// </summary>
		Completed,

		/// <summary>
		/// Operation is failed
		/// </summary>
		Failed,
	}

	public enum UserFinHistoryType {

		/// <summary>
		/// Gold purchase
		/// </summary>
		GoldBuy = 1,

		/// <summary>
		/// Gold selling
		/// </summary>
		GoldSell,

		/// <summary>
		/// GOLD transfer from HW
		/// </summary>
		HwTransfer,
	}

	public enum UserFinHistoryStatus {

		/// <summary>
		/// Initially created
		/// </summary>
		Unconfirmed = 1,

		/// <summary>
		/// Manual operation / sent to support team
		/// </summary>
		Manual,

		/// <summary>
		/// Pending
		/// </summary>
		Processing,

		/// <summary>
		/// Completed
		/// </summary>
		Completed,

		/// <summary>
		/// Failed
		/// </summary>
		Failed,
	}

	public enum SignedDocumentType {

		/// <summary>
		/// Terms of service (of sales)
		/// </summary>
		Tos = 1,

		/// <summary>
		/// Data privacy policy (agreement)
		/// </summary>
		Dpa,
	}

	public enum UserActivityType {

		/// <summary>
		/// User logged in
		/// </summary>
		Auth = 1,

		/// <summary>
		/// Password restoration / change
		/// </summary>
		Password,

		/// <summary>
		/// Some setting changed
		/// </summary>
		Settings,

		/// <summary>
		/// Credit card operations
		/// </summary>
		CreditCard,

		/// <summary>
		/// Exchange operations
		/// </summary>
		Exchange,
	}

    public enum PromoCodeUsageType
    {

        /// <summary>
        /// For one user
        /// </summary>
        Single = 1,

        /// <summary>
        /// For multiple users
        /// </summary>
        Multiple
    }


    #endregion

	//#region Token migration (Ethereum <-> Sumus)

	//public enum MigrationRequestStatus : int {

	//	/// <summary>
	//	/// Enqueued
	//	/// </summary>
	//	Initial = 1,

	//	/// <summary>
	//	/// Awaiting for transferring confirmation
	//	/// </summary>
	//	TransferConfirmation,

	//	/// <summary>
	//	/// Emission step
	//	/// </summary>
	//	Emission,

	//	/// <summary>
	//	/// Emission step started
	//	/// </summary>
	//	EmissionStarted,

	//	/// <summary>
	//	/// Awaiting for emission confirmation
	//	/// </summary>
	//	EmissionConfirmation,

	//	/// <summary>
	//	/// Done
	//	/// </summary>
	//	Completed,

	//	/// <summary>
	//	/// Failure
	//	/// </summary>
	//	Failed,
	//}

	//public enum SumusTransactionStatus {

	//	/// <summary>
	//	/// Unconfirmed status, still outside of any block
	//	/// </summary>
	//	Pending = 1,

	//	/// <summary>
	//	/// Transaction confirmed
	//	/// </summary>
	//	Success,

	//	/// <summary>
	//	/// Transaction cancelled or failed
	//	/// </summary>
	//	Failed,

	//	/// <summary>
	//	/// Stale transaction (still pending)
	//	/// </summary>
	//	Stale,
	//}

	//#endregion

	public enum FiatCurrency {

		Usd = 1,
		Eur = 2,
	}

	public enum CurrencyRateType {

		Unknown = 0,
		Gold = 1,
		Eth = 2,
	}

	public enum DbSetting {

		FeesTable = 1,
		RuntimeConfig,
		CryptoCapitalDepositData,
		GoldEthBuyHarvLastBlock,
		GoldEthSellHarvLastBlock,
		MigrationEthHarvLastBlock,
		MigrationSumHarvLastBlock,
		PoolFreezerHarvLastBlock,
	}

	public enum MutexEntity {

		/// <summary>
		/// Sending a notification (notification-wide)
		/// </summary>
		NotificationSend = 1,

		/// <summary>
		/// Hot wallet operation initiation mutex (user-wide)
		/// </summary>
		UserHwOperation,

		/// <summary>
		/// Processing ethereum opration (operation-wide)
		/// </summary>
		EthOperation,

		/// <summary>
		/// Changing buying request state (request-wide)
		/// </summary>
		GoldBuyingReq,

		/// <summary>
		/// Changing selling request state (request-wide)
		/// </summary>
		GoldSellingReq,

		/// <summary>
		/// Payment check (payment-wide)
		/// </summary>
		CardPaymentCheck,

		/// <summary>
		/// Wallet balance change (wallet-wide)
		/// </summary>
		SumusWalletBalance,
	}

	public enum NotificationType {

		Email = 1,
	}
}
