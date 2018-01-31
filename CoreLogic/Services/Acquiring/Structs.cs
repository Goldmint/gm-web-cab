using Goldmint.Common;
using System;
using System.Globalization;
using System.Net;

namespace Goldmint.CoreLogic.Services.Acquiring {

	public sealed class StartPaymentCardStore {

		public string RedirectUrl;

		public string TransactionId;
		public int AmountCents;
		public FiatCurrency Currency;
		public string Purpose;

		public string SenderName;
		public string SenderEmail;
		public string SenderPhone;
		public IPAddress SenderIP;

		public RegionInfo SenderAddressCountry;
		public string SenderAddressState;
		public string SenderAddressCity;
		public string SenderAddressStreet;
		public string SenderAddressZip;
	}

	public sealed class StartPaymentCharge {

		public string InitialGWTransactionId;
		public string TransactionId;

		public int AmountCents;
		public string Purpose;
		public string DynamicDescriptor;
	}

	public sealed class StartCreditCardStore {

		public string RedirectUrl;

		public string TransactionId;
		public int AmountCents;
		public FiatCurrency Currency;
		public string Purpose;

		public string RecipientName;
		public string RecipientEmail;
		public string RecipientPhone;
		public IPAddress RecipientIP;

		public RegionInfo RecipientAddressCountry;
		public string RecipientAddressState;
		public string RecipientAddressCity;
		public string RecipientAddressStreet;
		public string RecipientAddressZip;
	}

	public sealed class StartCreditCharge {

		public string InitialGWTransactionId;
		public string TransactionId;

		public int AmountCents;
		public string Purpose;
		public string DynamicDescriptor;
	}

	public sealed class StartP2PCardStore {

		public string RedirectUrl;

		public string TransactionId;
		public int AmountCents;
		public FiatCurrency Currency;
		public string Purpose;

		public string RecipientCardHolder;
		public string RecipientName;

		public string SenderName;
		public DateTime SenderBirthDate;
		public string SenderEmail;
		public string SenderPhone;
		public IPAddress SenderIP;

		public RegionInfo SenderAddressCountry;
		public string SenderAddressState;
		public string SenderAddressCity;
		public string SenderAddressStreet;
		public string SenderAddressZip;
	}

	public sealed class StartP2PCharge {

		public string InitialGWTransactionId;
		public string TransactionId;

		public int AmountCents;
		public string Purpose;
		public string DynamicDescriptor;
	}

	public sealed class StartCardStoreResult {

		public string GWTransactionId;
		public string Redirect;
	}

	public sealed class CheckStoreCardResult {

		public CardGatewayTransactionStatus Status;

		public string CardHolder;
		public string CardMask;

		public string ProviderMessage;
		public string ProviderStatus;
	}

	public sealed class ChargeResult {

		public bool Success;
		public string ProviderMessage;
		public string ProviderStatus;
	}

	public sealed class RefundPayment {

		public string TransactionId;
		public string RefGWTransactionId;
		public int AmountCents;
	}
}
