namespace Goldmint.CoreLogic.Services.The1StPayments {

	public sealed partial class The1StPayments {

		internal enum TransactionStatusId : int {

			Initialized = 1, // Initialized
			SentToProcessor = 2, // Sent to processor
			AmountHoldOK = 3, // Amount hold OK
			AmountHoldFailed = 4, // Amount hold failed
			SMSChargeFailed = 5, // SMS charge failed
			DMSChargeFailed = 6, // DMS charge failed
			Success = 7, // Success
			Expired = 8, // Expired
			HoldExpired = 9, // Hold expired
			RefundFailed = 11, // Refund failed
			RefundPending = 12, // Refund pending
			RefundSuccess = 13, // Refund success
			CardholderEntersCardData = 14, // Cardholder enters card data
			DMSCancelledOK = 15, // DMS canceled OK
			DMSCancelFailed = 16, // DMS cancel failed
			Reversed = 17, // Reversed
			CreditFailed = 18, // Credit failed
			CreditSuccess = 19, // Credit success
			P2PSuccess = 20, // P2P success
			P2PFailed = 21, // P2P failed
			CardDataStoreSMSSuccess = 22, // Card data store for SMS success
			CardDataStoreSMSFailed = 23, // Card data store for SMS failed
			CardDataStoreCRDSuccess = 24, // Card data store for CRD success
			CardDataStoreCRDFailed = 25, // Card data store for CRD failed
			CardDataStoreP2PSuccess = 26, // Card data store for P2P success
			CardDataStoreP2PFailed = 27, // Card data store for P2P failed
			InitializationCancelled = 28, // Transaction initialization was cancelled
			InitializationAutoCancelled = 29, // Transaction was automatically cancelled
		}

		internal enum CardSaveStatus : int {
			NotToBeSaved = 0, // Not to be saved
			NeedsToBeSaved = 1, // Needs to be saved
			SuccessfullySaved = 2, // Successfully saved
			FailedToSaveCard1 = 3, // Failed to save card
			FailedToSaveCard2 = 4, // Failed to save card
			SavedDataWillBeUsed = 5, // Saved data will be used
			NeedsToBeSavedOnMerchantSide = 6, // Needs to be saved on a merchant side
			SuccessfullySavedOnMerchantSide = 7, // Successfully saved on a merchant side
			FailedToSaveOnMerchantSide = 8, // Failed to save card on a merchant side
			NeedToBeSavedMOTO = 9, // Needs to be saved for MOTO
			SuccessfullySavedMOTO = 10, // Successfully saved for MOTO
			SavedDataWillBeUsedMOTO = 11, // Saved data will be used for MOTO
		}
	}
}
