using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Microsoft.AspNetCore.Http;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.CoreLogic.Services.KYC.Impl {

	public class ShuftiPro13KycProvider : IKycProvider {

		private readonly ShuftiPro13Options _opts;
		private readonly ILogger _logger;

		public ShuftiPro13KycProvider(Action<ShuftiPro13Options> setup, LogFactory logFactory) {
			_opts = new ShuftiPro13Options() { };
			setup(_opts);
			_logger = logFactory.GetLoggerFor(this);
		}

		// ---

		private bool CheckCallbackSignature(string rawJson, string signature) {
			if ((signature?.Length ?? 0) == 0) {
				return false;
			}
			if ((rawJson?.Length ?? 0) == 0) {
				return false;
			}
			return Hash.SHA256(rawJson + _opts.ClientSecret) == signature.ToLower();
		}

		internal class ServiceJsonResponse {

			public string reference { get; set; }
			public string @event { get; set; }
			public string verification_url { get; set; }
			public string email { get; set; }
		}

		// ---

		public async Task<string> GetRedirect(UserData user, string ticketId, string userRedirectUrl, string callbackUrl) {
			var ret = (string)null;

			var bodyJson = Json.Stringify(
				new {
					reference = ticketId,
					redirect_url = userRedirectUrl,
					callback_url = callbackUrl,
					email = user.Email,
					country = user.CountryCode,
					language = user.LanguageCode,
					verification_mode = "video_only",
					face = "",
					document = new {
						supported_types = new[] { "id_card", "driving_license", "passport" },
						name = new {
							first_name = user.FirstName,
							last_name = user.LastName,
						},
						dob = user.DoB.ToString("yyyy-MM-dd"),
					},
					// "+" + user.PhoneNumber.Trim('+')
				}
			);

			using (var req = new Request(_logger)) {
				await req
					.AcceptJson()
					.AuthBasic($"{_opts.ClientId}:{_opts.ClientSecret}", true)
					.BodyJson(bodyJson)
					.OnResult(async (res) => {
						var raw = await res.ToRawString();

						var sig = res.GetHeader("sp_signature");
						if (sig.Length != 1) {
							_logger?.Error("Missing signature");
							return;
						}

						var result = new ServiceJsonResponse();
						if (Json.ParseInto(raw, result) && CheckCallbackSignature(raw, sig[0])) {
							if (result.@event == "request.pending" && result.verification_url != "") {
								ret = result.verification_url;
							} else {
								_logger?.Error("Failed to get redirect: {0} with url: {1}", result.@event, result.verification_url);
							}
						} else {
							_logger?.Error("Failed to parse response or invalid signature");
						}
					})
					.SendPost("https://shuftipro.com/api/")
				;
			}

			return ret;
		}

		public async Task<CallbackResult> OnServiceCallback(HttpRequest request) {
			var ret = new CallbackResult() {
				OverallStatus = VerificationStatus.Pending,
			};

			try {
				var result = new ServiceJsonResponse();

				var raw = "";
				using (var reader = new StreamReader(request.Body)) {
					raw = await reader.ReadToEndAsync();
				}

				if (!request.Headers.TryGetValue("sp_signature", out var sigs) || sigs.Count != 1) {
					throw new Exception("Missing signature");
				}
				if (!CheckCallbackSignature(raw, sigs[0])) {
					throw new Exception("Invalid signature");
				}

				if (!Json.ParseInto(raw, result)) {
					throw new Exception("Failed to parse response");
				}

				if (result.@event != "request.pending") {
					ret.OverallStatus = result.@event == "verification.accepted"? VerificationStatus.Verified: VerificationStatus.NotVerified;
					ret.TicketId = result.reference;
					ret.ServiceStatus = result.@event;
					ret.ServiceMessage = result.@event;
				}

				_logger?.Info("Callback event is {0} for ref {1}", result.@event, result.reference);

			} catch (Exception e) {
				ret.OverallStatus = VerificationStatus.Fail;
				_logger?.Info(e, "Callback failure");
			}

			return ret;
		}
	}

	public sealed class ShuftiPro13Options {

		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}
}
