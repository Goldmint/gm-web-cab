using FluentValidation;
using Goldmint.Common;
using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.The1StPayments {

	public sealed partial class The1StPayments {

		private static readonly Regex RexDynamicDescriptorChars = new Regex("^[a-zA-Z0-9_:;]{1,21}$");

		private The1stPaymentsOptions _opts;
		private ILogger _logger;

		public The1StPayments(Action<The1stPaymentsOptions> options, LogFactory logFactory) {
			_opts = new The1stPaymentsOptions();
			options(_opts);
			_logger = logFactory.GetLoggerFor(this);
		}

		// ---

		/// <summary>
		/// Get redirect to save card for deposits
		/// </summary>
		/// <returns>Redirect or throws exception</returns>
		public async Task<StartCardStoreResult> StartPaymentCardStore3D(StartPaymentCardStore3D data) {

			try {

				// validate
				{
					var v = new InlineValidator<StartPaymentCardStore3D>();

					v.RuleFor(_ => _.RedirectUrl).Must(ValidationRules.BeValidUrl);

					v.RuleFor(_ => _.TransactionId).Length(5, 50);
					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(0);
					v.RuleFor(_ => _.Currency).NotNull();
					v.RuleFor(_ => _.Purpose).Length(5, 255);

					v.RuleFor(_ => _.SenderName).Length(2, 100);
					v.RuleFor(_ => _.SenderEmail).EmailAddress().Length(1, 100);
					v.RuleFor(_ => _.SenderPhone).Length(5, 25).Must(ValidationRules.BeValidPhone);
					v.RuleFor(_ => _.SenderIP).NotNull();

					v.RuleFor(_ => _.SenderAddressCountry).NotNull().Must(ValidationRules.BeValidCountryCodeAlpha2);
					v.RuleFor(_ => _.SenderAddressState).Length(2, 20);
					v.RuleFor(_ => _.SenderAddressCity).Length(2, 25);
					v.RuleFor(_ => _.SenderAddressStreet).Length(2, 50);
					v.RuleFor(_ => _.SenderAddressZip).Length(2, 15);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitStoreSms3D)
					.Set("save_card", "4")
					.Set("custom_return_url", data.RedirectUrl)

					.Set("merchant_transaction_id", data.TransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("currency", data.Currency.ToString().ToUpper())
					.Set("description", data.Purpose)

					.Set("name_on_card", data.SenderName)
					.Set("email", data.SenderEmail)
					.Set("phone", data.SenderPhone)
					.Set("user_ip", data.SenderIP.MapToIPv4().ToString())

					.Set("country", data.SenderAddressCountry)
					.Set("state", data.SenderAddressState)
					.Set("city", data.SenderAddressCity)
					.Set("street", data.SenderAddressStreet)
					.Set("zip", data.SenderAddressZip)
				;

				var pairs = await SendRequest("init", fields);

				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var txid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(txid)) {
					throw new Exception("Service transaction ID is empty");
				}

				var redirect = pairs.GetValueOrDefault("RedirectOnsite");
				if (string.IsNullOrWhiteSpace(redirect)) {
					throw new Exception("Redirect is empty");
				}

				return new StartCardStoreResult() {
					Redirect = redirect,
					GWTransactionId = txid,
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Start charging of saved card
		/// </summary>
		/// <returns>New transaction ID or throws exception</returns>
		public async Task<string> StartPaymentCharge3D(StartPaymentCharge3D data) {

			try {

				// validate
				{
					var v = new InlineValidator<StartPaymentCharge3D>();

					v.RuleFor(_ => _.InitialGWTransactionId).Length(40);
					v.RuleFor(_ => _.TransactionId).Length(5, 50);

					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);
					v.RuleFor(_ => _.Purpose).Length(5, 255);
					v.RuleFor(_ => _.DynamicDescriptor).Must(BeValidDynamicDescriptor).When(_ => _.DynamicDescriptor != null);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitRecurrent3D)
					.Set("merchant_transaction_id", data.TransactionId)
					.Set("original_init_id", data.InitialGWTransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("description", data.Purpose.ToString())
					.Set("use_saved_card", "1")
				;
				if (!string.IsNullOrWhiteSpace(data.DynamicDescriptor)) {
					fields.Set("merchant_referring_name", " " + data.DynamicDescriptor);
				}

				var pairs = await SendRequest("init", fields);
				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var txid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(txid)) {
					throw new Exception("Service transaction ID is empty");
				}

				return txid;
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Does charging
		/// </summary>
		/// <returns>Final result of charging or throws exception</returns>
		public async Task<ChargeResult> DoPaymentCharge3D(string gwTransactionId) {

			try {

				// validate
				if (string.IsNullOrWhiteSpace(gwTransactionId) || gwTransactionId.Length != 40) {
					throw new Exception("Illegal transaction ID format");
				}

				var fields = new Parameters()
					.Set("init_transaction_id", gwTransactionId)
					.Set("f_extended", "100")
				;

				var pairs = await SendRequest("charge", fields);
				if (pairs.ContainsKey("ERROR")) {
					return new ChargeResult() {
						Success = false,
						ProviderMessage = pairs["_RAW_"],
						ProviderStatus = "Error",
					};
				}

				var status = DeserializeTransactionStatus(pairs);
				return new ChargeResult() {
					Success = status.StatusId == TransactionStatusId.Success,
					ProviderMessage = status.FormatProviderMessage(),
					ProviderStatus = status.FormatProviderStatus(),
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		// ---

		/*

		NON-3D operations currently are unused
		
		/// <summary>
		/// Get redirect to save card for deposits
		/// </summary>
		/// <returns>Redirect or throws exception</returns>
		public async Task<StartCardStoreResult> StartPaymentCardStoreNon3D(StartPaymentCardStoreNon3D data) {

			try {

				// validate
				{
					var v = new InlineValidator<StartPaymentCardStoreNon3D>();

					v.RuleFor(_ => _.RedirectUrl).Must(ValidationRules.BeValidUrl);

					v.RuleFor(_ => _.TransactionId).Length(5, 50);
					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);
					v.RuleFor(_ => _.Currency).NotNull();
					v.RuleFor(_ => _.Purpose).Length(5, 255);

					v.RuleFor(_ => _.SenderName).Length(2, 100);
					v.RuleFor(_ => _.SenderEmail).EmailAddress().Length(1, 100);
					v.RuleFor(_ => _.SenderPhone).Length(5, 25).Must(ValidationRules.BeValidPhone);
					v.RuleFor(_ => _.SenderIP).NotNull();

					v.RuleFor(_ => _.SenderAddressCountry).NotNull().Must(ValidationRules.BeValidCountryCodeAlpha2);
					v.RuleFor(_ => _.SenderAddressState).Length(2, 20);
					v.RuleFor(_ => _.SenderAddressCity).Length(2, 25);
					v.RuleFor(_ => _.SenderAddressStreet).Length(2, 50);
					v.RuleFor(_ => _.SenderAddressZip).Length(2, 15);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitStoreSms)
					.Set("custom_return_url", data.RedirectUrl)

					.Set("merchant_transaction_id", data.TransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("currency", data.Currency.ToString().ToUpper())
					.Set("description", data.Purpose)

					.Set("name_on_card", data.SenderName)
					.Set("email", data.SenderEmail)
					.Set("phone", data.SenderPhone)
					.Set("user_ip", data.SenderIP.MapToIPv4().ToString())

					.Set("country", data.SenderAddressCountry)
					.Set("state", data.SenderAddressState)
					.Set("city", data.SenderAddressCity)
					.Set("street", data.SenderAddressStreet)
					.Set("zip", data.SenderAddressZip)
				;

				var pairs = await SendRequest("init_store_card_sms", fields);

				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var txid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(txid)) {
					throw new Exception("Service transaction ID is empty");
				}

				var redirect = pairs.GetValueOrDefault("RedirectOnsite");
				if (string.IsNullOrWhiteSpace(redirect)) {
					throw new Exception("Redirect is empty");
				}

				return new StartCardStoreResult() {
					Redirect = redirect,
					GWTransactionId = txid,
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Start charging of saved card
		/// </summary>
		/// <returns>New transaction ID or throws exception</returns>
		public async Task<string> StartPaymentChargeNon3D(StartPaymentChargeNon3D data) {

			try {

				// validate
				{
					var v = new InlineValidator<StartPaymentChargeNon3D>();

					v.RuleFor(_ => _.InitialGWTransactionId).Length(40);
					v.RuleFor(_ => _.TransactionId).Length(5, 50);

					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);
					v.RuleFor(_ => _.Purpose).Length(5, 255);
					v.RuleFor(_ => _.DynamicDescriptor).Must(BeValidDynamicDescriptor).When(_ => _.DynamicDescriptor != null);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitRecurrent)
					.Set("merchant_transaction_id", data.TransactionId)
					.Set("original_init_id", data.InitialGWTransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("description", data.Purpose.ToString())
				;
				if (!string.IsNullOrWhiteSpace(data.DynamicDescriptor)) {
					fields.Set("merchant_referring_name", " " + data.DynamicDescriptor);
				}

				var pairs = await SendRequest("init_recurrent", fields);
				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var txid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(txid)) {
					throw new Exception("Service transaction ID is empty");
				}

				return txid;
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Does charging
		/// </summary>
		/// <returns>Final result of charging or throws exception</returns>
		public async Task<ChargeResult> DoPaymentChargeNon3D(string gwTransactionId) {

			try {

				// validate
				if (string.IsNullOrWhiteSpace(gwTransactionId) || gwTransactionId.Length != 40) {
					throw new Exception("Illegal transaction ID format");
				}

				var fields = new Parameters()
					.Set("init_transaction_id", gwTransactionId)
					.Set("f_extended", "100")
				;

				var pairs = await SendRequest("charge_recurrent", fields);
				if (pairs.ContainsKey("ERROR")) {
					return new ChargeResult() {
						Success = false,
						ProviderMessage = pairs["_RAW_"],
						ProviderStatus = "Error",
					};
				}

				var status = DeserializeTransactionStatus(pairs);
				return new ChargeResult() {
					Success = status.StatusId == TransactionStatusId.Success,
					ProviderMessage = status.FormatProviderMessage(),
					ProviderStatus = status.FormatProviderStatus(),
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}
		
		*/

		// ---

		/// <summary>
		/// Get redirect to save card for credit payments
		/// </summary>
		/// <returns>Redirect or throws exception</returns>
		public async Task<StartCardStoreResult> StartCreditCardStore(StartCreditCardStore data) {

			try {

				// validation
				{
					var v = new InlineValidator<StartCreditCardStore>();

					v.RuleFor(_ => _.RedirectUrl).Must(ValidationRules.BeValidUrl);

					v.RuleFor(_ => _.TransactionId).Length(5, 50);
					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);
					v.RuleFor(_ => _.Currency).NotNull();
					v.RuleFor(_ => _.Purpose).Length(5, 255);

					v.RuleFor(_ => _.RecipientName).Length(2, 25); // visa = 25, mc = 30, general = 100
					v.RuleFor(_ => _.RecipientEmail).EmailAddress().Length(1, 100);
					v.RuleFor(_ => _.RecipientPhone).Length(5, 25).Must(ValidationRules.BeValidPhone);
					v.RuleFor(_ => _.RecipientIP).NotNull();

					v.RuleFor(_ => _.RecipientAddressCountry).NotNull().Must(ValidationRules.BeValidCountryCodeAlpha2);
					v.RuleFor(_ => _.RecipientAddressState).Length(2, 20);
					v.RuleFor(_ => _.RecipientAddressCity).Length(2, 25);
					v.RuleFor(_ => _.RecipientAddressStreet).Length(2, 30); // visa = 30, mc = 35, general = 50
					v.RuleFor(_ => _.RecipientAddressZip).Length(2, 10); // 10, but general is 15

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitStoreCrd)
					.Set("custom_return_url", data.RedirectUrl)

					.Set("merchant_transaction_id", data.TransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("currency", data.Currency.ToString().ToUpper())
					.Set("description", data.Purpose)

					.Set("name_on_card", data.RecipientName)
					.Set("email", data.RecipientEmail)
					.Set("phone", data.RecipientPhone)
					.Set("user_ip", data.RecipientIP.MapToIPv4().ToString())

					.Set("country", data.RecipientAddressCountry)
					.Set("state", data.RecipientAddressState)
					.Set("city", data.RecipientAddressCity)
					.Set("street", data.RecipientAddressStreet)
					.Set("zip", data.RecipientAddressZip)
				;

				var pairs = await SendRequest("init_store_card_credit", fields);
				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var gwtid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(gwtid)) {
					throw new Exception("Service transaction ID is empty");
				}

				var redirect = pairs.GetValueOrDefault("RedirectOnsite");
				if (string.IsNullOrWhiteSpace(redirect)) {
					throw new Exception("Redirect is empty");
				}

				return new StartCardStoreResult() {
					Redirect = redirect,
					GWTransactionId = gwtid,
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Start payment to saved card
		/// </summary>
		/// <returns>New transaction ID or throws exception</returns>
		public async Task<string> StartCreditCharge(StartCreditCharge data) {

			try {
				// validate
				{
					var v = new InlineValidator<StartCreditCharge>();

					v.RuleFor(_ => _.InitialGWTransactionId).Length(40);
					v.RuleFor(_ => _.TransactionId).Length(5, 50);

					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);
					v.RuleFor(_ => _.Purpose).Length(5, 255);
					v.RuleFor(_ => _.DynamicDescriptor).Must(BeValidDynamicDescriptor).When(_ => _.DynamicDescriptor != null);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("rs", _opts.RsInitRecurrentCrd)
					.Set("merchant_transaction_id", data.TransactionId)
					.Set("original_init_id", data.InitialGWTransactionId)
					.Set("amount", data.AmountCents.ToString())
					.Set("description", data.Purpose.ToString())
				;
				if (!string.IsNullOrWhiteSpace(data.DynamicDescriptor)) {
					fields.Set("merchant_referring_name", " " + data.DynamicDescriptor);
				}

				var pairs = await SendRequest("init_recurrent_credit", fields);
				if (pairs.ContainsKey("ERROR")) {
					throw new Exception($"Error response: `{pairs["_RAW_"]}`");
				}

				var gwtid = pairs.GetValueOrDefault("OK");
				if (string.IsNullOrWhiteSpace(gwtid)) {
					throw new Exception("Service transaction ID is empty");
				}

				return gwtid;
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		/// <summary>
		/// Does payment
		/// </summary>
		/// <returns>Final result of payment or throws exception</returns>
		public async Task<ChargeResult> DoCreditCharge(string gwTransactionId) {

			try {

				// validate
				if (string.IsNullOrWhiteSpace(gwTransactionId) || gwTransactionId.Length != 40) {
					throw new Exception("Illegal transaction ID format");
				}

				var fields = new Parameters()
					.Set("init_transaction_id", gwTransactionId)
					.Set("f_extended", "100")
				;

				var pairs = await SendRequest("do_recurrent_credit", fields);
				if (pairs.ContainsKey("ERROR")) {
					return new ChargeResult() {
						Success = false,
						ProviderMessage = pairs["_RAW_"],
						ProviderStatus = "Error",
					};
				}

				var status = DeserializeTransactionStatus(pairs);
				return new ChargeResult() {
					Success = status.StatusId == TransactionStatusId.CreditSuccess,
					ProviderMessage = status.FormatProviderMessage(),
					ProviderStatus = status.FormatProviderStatus(),
				};
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		// ---

		/// <summary>
		/// Refund deposit payment
		/// </summary>
		/// <returns>GW transaction ID on success or null on failure</returns>
		public async Task<string> RefundPayment(RefundPayment data) {

			try {

				// validation
				{
					var v = new InlineValidator<RefundPayment>();

					v.RuleFor(_ => _.RefGWTransactionId).Length(40);
					v.RuleFor(_ => _.TransactionId).Length(5, 50);
					v.RuleFor(_ => _.AmountCents).GreaterThanOrEqualTo(100);

					v.ValidateAndThrow(data);
				}

				var fields = new Parameters()
					.Set("guid", "")
					.Set("account_guid", _opts?.MerchantGuid)
					.Set("init_transaction_id", data.RefGWTransactionId)
					.Set("amount_to_refund", data.AmountCents.ToString())
					.Set("merchant_transaction_id", data.TransactionId)
					.Set("details", "true")
				;

				var pairs = await SendRequest("refund", fields);

				// ok
				if (pairs.ContainsKey("Refund Success")) {
					return pairs.GetValueOrDefault("internal_refund_id");
				}

				return null;
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to start transaction");
				throw e;
			}
		}

		// ---

		private sealed class CheckTransactionResult {

			public string GWTransactionId { get; internal set; }
			public string TransactionId { get; internal set; }

			public TransactionStatusId StatusId { get; internal set; }
			public CardGatewayTransactionStatus OverallStatus { get; internal set; }

			public string CardHolder { get; internal set; }
			public string CardMask { get; internal set; }
			public string CardSaveStatus { get; internal set; }

			public string ChargeResultCode { get; internal set; }
			public string ChargeDetails { get; internal set; }
			public string ChargeResultCodeString { get; internal set; }
			public string ProcessorError { get; internal set; }

			public string Warning { get; internal set; }

			public string FormatProviderMessage() {
				return (""
					+ (ChargeResultCodeString ?? "") + " / "
					+ (ChargeResultCode ?? "") + " / "
					+ (ProcessorError ?? "") + " / "
				).Trim('/', ' ');
			}

			public string FormatProviderStatus() {
				return $"{StatusId.ToString()};{(int)StatusId}";
			}
		}

		/// <summary>
		/// General transaction status request
		/// </summary>
		/// <returns>Result from GW, null if transaction not found or throws exception</returns>
		private async Task<CheckTransactionResult> GetTransactionStatus(string gwTransactionId) {

			try {

				// validate
				if (string.IsNullOrWhiteSpace(gwTransactionId) || gwTransactionId.Length != 40) {
					throw new Exception("Illegal transaction ID format");
				}

				var fields = new Parameters()
					.Set("request_type", "transaction_status")
					.Set("init_transaction_id", gwTransactionId)
					// or by merchant tid : Set("merchant_transaction_id", transactionId);
					.Set("f_extended", "100")
				;

				var pairs = await SendRequest("status_request", fields);

				// error
				if (pairs.ContainsKey("ERROR")) {
					if (pairs["_RAW_"].Contains("Unknown merchant transaction ID")) {
						return null;
					}
					else {
						throw new Exception("Got error response");
					}
				}

				var result = DeserializeTransactionStatus(pairs);

				// set overall status
				switch (result.StatusId) {

					case TransactionStatusId.Success:
					case TransactionStatusId.RefundSuccess:
					case TransactionStatusId.AmountHoldOK:
					case TransactionStatusId.DMSCancelledOK:
					case TransactionStatusId.CreditSuccess:
					case TransactionStatusId.CardDataStoreSMSSuccess:
					case TransactionStatusId.CardDataStoreCRDSuccess:
					case TransactionStatusId.CardDataStoreP2PSuccess:
					case TransactionStatusId.P2PSuccess:
						result.OverallStatus = CardGatewayTransactionStatus.Success;
						break;


					case TransactionStatusId.Expired:
					case TransactionStatusId.AmountHoldFailed:
					case TransactionStatusId.SMSChargeFailed:
					case TransactionStatusId.DMSChargeFailed:
					case TransactionStatusId.HoldExpired:
					case TransactionStatusId.RefundFailed:
					case TransactionStatusId.DMSCancelFailed:
					case TransactionStatusId.CreditFailed:
					case TransactionStatusId.P2PFailed:
					case TransactionStatusId.CardDataStoreP2PFailed:
					case TransactionStatusId.CardDataStoreSMSFailed:
					case TransactionStatusId.CardDataStoreCRDFailed:
					case TransactionStatusId.InitializationCancelled:
					case TransactionStatusId.InitializationAutoCancelled:
						result.OverallStatus = CardGatewayTransactionStatus.Failed;
						break;

					default:
					case TransactionStatusId.Initialized:
					case TransactionStatusId.SentToProcessor:
					case TransactionStatusId.RefundPending:
					case TransactionStatusId.CardholderEntersCardData:
					case TransactionStatusId.Reversed:
						result.OverallStatus = CardGatewayTransactionStatus.Pending;
						break;
				}

				// in case of warning
				if (!string.IsNullOrWhiteSpace(result.Warning)) {
					// TODO: notify admin
				}

				return result;
			}
			catch (Exception e) {
				_logger?.Error(e, "[1STP] Failed to get transaction status");
				throw e;
			}
		}

		/// <summary>
		/// Check if payment, credit or p2p card stored
		/// </summary>
		public async Task<CheckStoreCardResult> CheckCardStored(string gwStoreTransactionId) {
			var result = await GetTransactionStatus(gwStoreTransactionId);

			// not found
			if (result == null) {
				_logger?.Error($"[1STP] Transaction not found: {gwStoreTransactionId}");

				return new CheckStoreCardResult() {
					Status = CardGatewayTransactionStatus.NotFound,
				};
			}

			// failed
			switch (result.CardSaveStatus) {
				case "2": // Successfully saved
				case "7": // Successfully saved on a merchant side
				case "10": // Successfully saved for MOTO
					break;

				case "0": // Not to be saved
				case "1": // Needs to be saved
				case "3": // Failed to save card
				case "4": // Failed to save card
				case "5": // Saved data will be used
				case "6": // Needs to be saved on a merchant side
				case "8": // Failed to save card on a merchant side
				case "9": // Needs to be saved for MOTO
				case "11": //Saved data will be used for MOTO
				default:
					_logger?.Error($"[1STP] Card has not been saved (status {result.CardSaveStatus}) for transaction {gwStoreTransactionId}");
					return new CheckStoreCardResult() {
						Status = CardGatewayTransactionStatus.Failed,
					};
			}

			return new CheckStoreCardResult() {
				Status = result.OverallStatus,

				CardHolder = result.CardHolder,
				CardMask = result.CardMask,

				ProviderStatus = result.FormatProviderStatus(),
				ProviderMessage = result.FormatProviderMessage(),
			};
		}

		// ---

		private async Task<Dictionary<string, string>> SendRequest(string method, Parameters postParams) {

			var queryParams = new Parameters()
				.Set("a", method)
			;

			postParams
				.SetIfEmpty("guid", _opts.MerchantGuid)
				.SetIfEmpty("pwd", Hash.SHA1(_opts.ProcessingPassword))
			;

			var ret = new Dictionary<string, string>();

			using (var req = new Request(_logger)) {
				await req
					.AcceptJson()
					.Query(queryParams)
					.BodyForm(postParams)
					.OnResult(async (res) => {
						var raw = await res.ToRawString();

						_logger?.Info($"[1STP] Method {method} responded with status `{res.GetHttpStatus()}`. Raw: `" + raw + "`");

						if (res.GetHttpStatus() == null || res.GetHttpStatus().Value != HttpStatusCode.OK) {
							throw new Exception($"Unexpected status code received: {res.GetHttpStatus().Value}");
						}
						if (string.IsNullOrWhiteSpace(raw)) {
							throw new Exception("Got empty or null request result");
						}

						ret = ParseResponse(raw);
						if (ret.Count == 0) {
							throw new Exception("Got empty or null request result (empty pairs)");
						}
						ret.Add("_RAW_", raw);
					})
					.SendPost(_opts.Gateway, TimeSpan.FromSeconds(90))
				;
			}

			return ret;
		}

		private Dictionary<string, string> ParseResponse(string raw) {
			var rawPairs = new Dictionary<string, string>();
			Array.ForEach(raw.Split('~'), x => {
				var pair = x.Split(':', 2);
				if (pair.ElementAtOrDefault(0) != null) {
					rawPairs.Add(pair.ElementAtOrDefault(0), pair.ElementAtOrDefault(1));
				}
			});
			return rawPairs;
		}

		private CheckTransactionResult DeserializeTransactionStatus(Dictionary<string, string> rawPairs) {

			var ret = new CheckTransactionResult() {};

			ret.OverallStatus = CardGatewayTransactionStatus.Pending;

			// Gateway transaction ID
			ret.GWTransactionId = rawPairs.GetValueOrDefault("ID");

			// Merchant transaction ID
			ret.TransactionId = rawPairs.GetValueOrDefault("MerchantID");

			// Transaction status
			// rawPairs.GetValueOrDefault("Status";

			// Numeric representation of transaction status
			if (Enum.TryParse(rawPairs.GetValueOrDefault("StatusID"), out TransactionStatusId statusId)) {
				ret.StatusId = statusId;
			}
			else {
				throw new Exception("Failed to parse status id");
			}

			// Status of card data in the TransactPro internal system for further operations without card data input from a card holder
			ret.CardSaveStatus = rawPairs.GetValueOrDefault("CardSaveStatus");

			// A merchant_referring_name parameter’s value from a transaction request, if a dynamic descriptor was used
			// rawPairs.GetValueOrDefault("MerchantReferringName"

			// Full formed dynamic descriptor, if merchant_referring_name parameter was passed into transaction
			// rawPairs.GetValueOrDefault("DynamicDescriptor";

			// Descriptor a cardholder will see in a bank statement
			// rawPairs.GetValueOrDefault("Terminal"

			// Country of the card issuer bank, two-letter ISO-3166 code. XX or an empty string will be passed if the issuer country was not found
			// rawPairs.GetValueOrDefault("CardIssuerCountry"

			// Name, passed as name printed on card
			ret.CardHolder = rawPairs.GetValueOrDefault("NameOnCard")?.ToUpper();

			// Card number, in 4111********1111 format
			ret.CardMask = rawPairs.GetValueOrDefault("CardMasked");

			// 3-character code of a transaction charge details
			ret.ChargeResultCode = rawPairs.GetValueOrDefault("ResultCode");

			// Transaction charge details converted into the string with it's description, given in cases if ResultCode is not set(In this case, return can be longer than 256 bytes)
			ret.ChargeDetails = rawPairs.GetValueOrDefault("ExtendedErrorCode");

			// 3-character code of transaction charge details converted into the string with it's description(In this case, return can be longer than 256 bytes)
			ret.ChargeResultCodeString = rawPairs.GetValueOrDefault("ResultCodeStr");

			// Contains an error, obtained from a processor if transaction has unsuccessful status.Will be empty for successful transaction.
			ret.ProcessorError = rawPairs.GetValueOrDefault("ProcessorError");

			// Warning
			ret.Warning = rawPairs.GetValueOrDefault("Warning");

			return ret;
		}

		private static bool BeValidDynamicDescriptor(string x) {
			return x != null && RexDynamicDescriptorChars.IsMatch(x);
		}

	}

	public sealed class The1stPaymentsOptions {

		public string MerchantGuid { get; set; }
		public string ProcessingPassword { get; set; }
		public string Gateway { get; set; }
		public string RsInitStoreSms3D { get; set; }
		public string RsInitRecurrent3D { get; set; }
		public string RsInitStoreSms { get; set; }
		public string RsInitRecurrent { get; set; }
		public string RsInitStoreCrd { get; set; }
		public string RsInitRecurrentCrd { get; set; }
		public string RsInitStoreP2P { get; set; }
		public string RsInitRecurrentP2P { get; set; }
	}
}
