using System;

namespace Goldmint.Common {

	#region Auth

	public enum Locale {

		En = 1,
	}

	public enum LoginProvider {

		Google = 1,
	}

	public enum JwtAudience {

		/// <summary>
		/// App access
		/// </summary>
		Cabinet = 1,
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
	}

	public enum UserTier {

		Tier0 = 0,
		Tier1 = 1,
		Tier2 = 2,
	}

	#endregion

	#region Buy/Sell GOLD

	public enum BuySellGoldRequestStatus {

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

	#region Mint (Sumus) blockchain

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

	#region User

	public enum UserFinHistoryType {

		GoldBuy = 1,
		GoldSell,
		GoldDeposit,
		GoldWithdraw
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
	
	public enum TradableCurrency {

		Eth = 1,
	}

	public enum FiatCurrency {

		Usd = 1,
		Eur = 2,
	}

	public enum CurrencyPrice {

		Gold = 1,
		Eth = 2,
	}

	public enum DbSetting {

		RuntimeConfig = 1,
		PoolFreezerHarvLastBlock,
	}

	public enum NotificationType {

		Email = 1,
	}
}
