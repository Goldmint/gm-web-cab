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
		/// Two factor auth area
		/// </summary>
		TFA,

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
		RestorePassword
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
		/// Dashboard: swift tab write access
		/// </summary>
		SwiftDepositWriteAccess = 0x8000000L,

		/// <summary>
		/// Dashboard: swift tab write access
		/// </summary>
		SwiftWithdrawWriteAccess = 0x10000000L,

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
		Tier1,
		Tier2,
	}

	#endregion


	#region Credit Card / Bank Account
	// REMOVE
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
		/// Awaiting for code from bank statement
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
		/// Initial state, just enqueued
		/// </summary>
		Pending = 1,

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

		/// <summary>
		/// Cancelled
		/// </summary>
		Cancelled,
	}

	#endregion


	#region GOLD

	public enum BuyGoldRequestInput {

		EthereumDirectPayment = 1,
	}

	public enum BuyGoldRequestDestination {

		/// <summary>
		/// Buy to Ethereum address
		/// </summary>
		EthereumAddress = 1,

		/// <summary>
		/// Buy to internal Hot Wallet
		/// </summary>
		EthereumHotWallet,
	}

	public enum BuyGoldRequestStatus {

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
		/// Expired
		/// </summary>
		Expired,

		/// <summary>
		/// Final failure
		/// </summary>
		Failed,
	}

	public enum TransferGoldRequestStatus {

		/// <summary>
		/// Initially enqueued
		/// </summary>
		Unconfirmed = 1,
		
		/// <summary>
		/// Confirmed by user
		/// </summary>
		Confirmed,

		/// <summary>
		/// Prepared for processing
		/// </summary>
		Prepared,

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

	/*
	public enum SwiftPaymentType {

		/// <summary>
		/// Deposit
		/// </summary>
		Deposit = 1,

		/// <summary>
		/// Withdraw
		/// </summary>
		Withdraw
	}

	public enum SwiftPaymentStatus {

		/// <summary>
		/// Awaiting payment
		/// </summary>
		Pending = 1,

		/// <summary>
		/// Completed
		/// </summary>
		Success,

		/// <summary>
		/// Cancelled by support
		/// </summary>
		Cancelled,
	}

	public enum DepositSource {

		/// <summary>
		/// Deposit comes from credit card
		/// </summary>
		CreditCard = 1,

		/// <summary>
		/// Deposit comes from bank transaction
		/// </summary>
		SwiftRequest,

		/// <summary>
		/// Deposit comes from failed withdrawal
		/// </summary>
		FailedWithdraw,

		/// <summary>
		/// Deposit initiated with cryptoasset's selling
		/// </summary>
		CryptoDeposit,
	}

	

	public enum WithdrawDestination {

		/// <summary>
		/// Destination is credit card
		/// </summary>
		CreditCard = 1,

		/// <summary>
		/// Destination is bank transaction
		/// </summary>
		Swift,

		/// <summary>
		/// Destination is cryptoasset's transfer
		/// </summary>
		CryptoExchange,
	}

	public enum WithdrawStatus {

		/// <summary>
		/// Initially enqueued
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
		/// Payment processing (actually used by card payment to wait for payment success)
		/// </summary>
		Processing,

		/// <summary>
		/// Final success
		/// </summary>
		Success,

		/// <summary>
		/// Final failure
		/// </summary>
		Failed,
	}

	public enum BuyGoldForAssetOrigin {

		/// <summary>
		/// Ethereum
		/// </summary>
		ETH = 1,
	}

	public enum CryptoDepositStatus {

		/// <summary>
		/// Just created, unconfirmed
		/// </summary>
		Unconfirmed = 1,

		/// <summary>
		/// Confirmed, awaiting for transaction
		/// </summary>
		Confirmed,
		
		/// <summary>
		/// Prepared for processing
		/// </summary>
		Prepared,

		/// <summary>
		/// Prepared for processing
		/// </summary>
		Processing,

		/// <summary>
		/// Success
		/// </summary>
		Success,

		/// <summary>
		/// Cancelled
		/// </summary>
		Failed,
	}
	*/
	#endregion


	#region Blockchain

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
		Settings
	}

	#endregion

	public enum CryptoCurrency {

		ETH = 1,
	}

	public enum FiatCurrency {

		USD = 1,
	}

	public enum DbSetting {

		LastExchangeIndex = 1,
		LastCryptoExchangeBlockChecked,
		FeesTable,
		CryptoCapitalDepositData,
	}

	public enum MutexEntity {
		
		/// <summary>
		/// Particular payment should be locked before it will be updated via acquirer (payment-wide)
		/// </summary>
		CardPaymentCheck = 1,

		/// <summary>
		/// Lock should be set while new deposit creation to check limits (user-wide)
		/// </summary>
		DepositEnqueue,

		/// <summary>
		/// Lock should be set while updating deposit (deposit-wide)
		/// </summary>
		DepositCheck,

		/// <summary>
		/// Lock should be set while new withdraw creation to check limits (user-wide)
		/// </summary>
		WithdrawEnqueue,

		/// <summary>
		/// Lock should be set while updating withdraw (withdraw-wide)
		/// </summary>
		WithdrawCheck,

		/// <summary>
		/// Lock should be set while sending a notification (notification-wide)
		/// </summary>
		NotificationSend,

		/// <summary>
		/// Lock should be set while changing buying request state
		/// </summary>
		EthBuyRequest,

		/// <summary>
		/// Lock should be set while changing selling request state
		/// </summary>
		EthSellRequest,

		/// <summary>
		/// Hot wallet operation initiation mutex
		/// </summary>
		HWOperation,

		/// <summary>
		/// Lock should be set while buying gold to hot wallet
		/// </summary>
		HWBuyRequest,

		/// <summary>
		/// Lock should be set while selling gold from hot wallet
		/// </summary>
		HWSellRequest,

		/// <summary>
		/// Lock should be set while transferring gold from hot wallet
		/// </summary>
		HWTransferRequest,

		/// <summary>
		/// Support team should lock SWIFT request record before it could be processed
		/// </summary>
		SupportSwiftRequestProc,

		/// <summary>
		/// Lock should be set while changing deposit request state
		/// </summary>
		CryptoDepositRequest,

		/// <summary>
		/// Lock should be set while confirming cryptoexchange opertations (crypto-deposit/crypto-withdraw)
		/// </summary>
		CryptoExchangeConfirm,
	}

	public enum NotificationType {

		Email = 1,
	}

}
