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
		/// Dashboard: _
		/// </summary>
		// _ = 0x8000000L,

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
		Tier1,
		Tier2,
	}

	#endregion


	#region Buy GOLD

	public enum BuyGoldRequestInput {

		/// <summary>
		/// User sends ETH to Ethereum contact
		/// </summary>
		ContractEthPayment = 1,
		
		/// <summary>
		/// User spends fiat from credit card
		/// </summary>
		CreditCardDeposit,
	}

	public enum BuyGoldRequestOutput {

		/// <summary>
		/// Issue gold to the Ethereum address
		/// </summary>
		EthereumAddress = 1,
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
		/// Final expired
		/// </summary>
		Expired,

		/// <summary>
		/// Final failure
		/// </summary>
		Cancelled,

		/// <summary>
		/// Final failure
		/// </summary>
		Failed,
	}

	#endregion


	#region Sell GOLD

	public enum SellGoldRequestInput {

		/// <summary>
		/// User burns GOLD at Ethereum contract
		/// </summary>
		ContractGoldBurning = 1,
	}

	public enum SellGoldRequestOutput {

		/// <summary>
		/// Send ETH to the address
		/// </summary>
		EthAddress = 1,

		/// <summary>
		/// Send fiat to the card
		/// </summary>
		CreditCard,
	}

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
		/// Expired
		/// </summary>
		Expired,

		/// <summary>
		/// Final failure
		/// </summary>
		Cancelled,

		/// <summary>
		/// Final failure
		/// </summary>
		Failed,

		/// <summary>
		/// Checking operation on blockchain
		/// </summary>
		EthConfirmation,
	}

	#endregion


	#region Support

	public enum SupportRequestStatus {

		/// <summary>
		/// Pending
		/// </summary>
		Pending = 1,

		/// <summary>
		/// Completed
		/// </summary>
		Success,

		/// <summary>
		/// Cancelled
		/// </summary>
		Cancelled,
	}

	#endregion


	#region Ethereum blockchain

	public enum EthereumOperationType {

		/// <summary>
		/// Transfer GOLD from HW to client address
		/// </summary>
		TransferGoldFromHw = 1,

		/// <summary>
		/// Call contract for request processing (ETH)
		/// </summary>
		ContractProcessBuyRequestEth,

		/// <summary>
		/// Call contract for request cancellation
		/// </summary>
		ContractCancelBuyRequest,

		/// <summary>
		/// Call contract for request processing (ETH)
		/// </summary>
		ContractProcessSellRequestEth,

		/// <summary>
		/// Call contract for request cancellation
		/// </summary>
		ContractCancelSellRequest,

		/// <summary>
		/// Call contract for request processing (fiat)
		/// </summary>
		ContractProcessBuyRequestFiat,

		/// <summary>
		/// Call contract for request processing (fiat)
		/// </summary>
		ContractProcessSellRequestFiat,
	}

	public enum EthereumOperationStatus {

		/// <summary>
		/// Enqueued
		/// </summary>
		Initial = 1,

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
	}

	#endregion


	public enum CryptoCurrency {

		Eth = 1,
	}

	public enum FiatCurrency {

		Usd = 1,
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
		/// Support exchange request state (request-wide)
		/// </summary>
		SupportBuyRequestProc,
		SupportSellRequestProc,
	}

	public enum NotificationType {

		Email = 1,
	}

}
