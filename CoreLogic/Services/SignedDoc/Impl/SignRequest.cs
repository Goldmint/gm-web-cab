using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Microsoft.AspNetCore.Http;
using NLog;
using System.IO;

namespace Goldmint.CoreLogic.Services.SignedDoc.Impl {

	public class SignRequest : IDocSigningProvider {

		private readonly string _baseUrl;
		private readonly string _authString;
		private readonly string _senderEmail;
		private readonly string _senderEmailName;

		private readonly ILogger _logger;
		private readonly IList<TemplateDesc> _templates;

		public SignRequest(string baseUrl, string authString, string senderEmail, string senderEmailName, LogFactory logFactory) {
			_baseUrl = baseUrl.TrimEnd('/');
			_authString = authString;
			_senderEmail = senderEmail;
			_senderEmailName = senderEmailName;
			_logger = logFactory.GetLoggerFor(this);
			_templates = new List<TemplateDesc>();
		}

		public void AddTemplate(string name, string filename, string templateUrl) {
			_templates.Add(new TemplateDesc() {
				Name = name,
				Filename = filename,
				TemplateUrl = templateUrl.TrimEnd('/') + "/",
			});
		}

		public async Task<bool> SendPrimaryAgreementRequest(string refId, string name, string email, DateTime date) {

			var template = _templates.FirstOrDefault(_ => _.Name == SignedDocumentType.GoldmintTOS.ToString());
			if (template == null) {
				_logger.Error($"Template not found for {nameof(SendPrimaryAgreementRequest)}");
				return false;
			}

			var jsonRequest = new {
				template = template.TemplateUrl,
				from_email = _senderEmail,
				from_email_name = _senderEmailName,
				//message = emailMessage,
				external_id = refId,
				name = template.Filename,
				signers = new[] { new { email = email } },
				prefill_tags = new[] {
					new { external_id = "field_date", text = (string)null, date_value = date.ToString("yyyy-MM-dd") },
					new { external_id = "field_client_name", text = name, date_value = (string)null },
				},
			};

			var success = false;
			var url = _baseUrl + "/signrequest-quick-create/";

			using (var req = new Request(_logger)) {
				await req
					.AcceptJson()
					.AuthToken(_authString)
					.BodyJson( Json.Stringify(jsonRequest) )
					.OnResult((res) => {
						var status = res.GetHttpStatus();
						//var jsonStr = res.ToRawString();
						if (status == System.Net.HttpStatusCode.OK || status == System.Net.HttpStatusCode.Created) {
							success = true;
						}
					})
					.SendPost(url, TimeSpan.FromSeconds(120))
				;
			}

			return success;
		}

		public async Task<CallbackResult> OnServiceCallback(HttpRequest request) {

			var ret = new CallbackResult() {
				OverallStatus = OverallStatus.Pending,
			};

			try {
				var data = new CallbackJsonData();

				var raw = "";
				using (var reader = new StreamReader(request.Body)) {
					raw = await reader.ReadToEndAsync();
				}

				if (!Json.ParseInto(raw, data) || data.event_hash == null || data.document?.external_id == null) {
					throw new Exception("Failed to parse response");
				}

				if (!CheckCallbackSignature(data, _authString)) {
					throw new Exception("Invalid signature");
				}

				ret.ReferenceId = data.document.external_id;
				
				// ok
				if (data.status == "ok" && data.event_type == "signed") {
					ret.OverallStatus = OverallStatus.Signed;
				}

				// failed / declined
				if (data.status == "error" || data.event_type == "declined" || data.event_type == "cancelled") {
					ret.OverallStatus = OverallStatus.Declined;
				}

				ret.ServiceStatus = data.status;
				ret.ServiceMessage = data.event_type;

				_logger?.Info($"Callback status '{data.status}', event '{data.event_type}' for ref {data.document.external_id}");
			}
			catch (Exception e) {
				ret.OverallStatus = OverallStatus.Error;
				_logger?.Info(e, "Callback failure");
			}

			return ret;
		}

		private bool CheckCallbackSignature(CallbackJsonData data, string tokenUsed) {
			if (string.IsNullOrWhiteSpace(data?.event_hash)) return false;
			// TODO: signature source doesnt contain some document ID
			var chk = (data?.event_time ?? "") + (data?.event_type ?? "");
			return Common.Hash.HMACSHA256(chk, tokenUsed) == data.event_hash;
		}

		// ---

		private class TemplateDesc {
			public string Name { get; set; }
			public string Filename { get; set; }
			public string TemplateUrl { get; set; }
		}

		internal class CallbackJsonData {

			public DocumentSection document { get; set; }
			public string event_hash { get; set; }
			public string event_time { get; set; }
			public string event_type { get; set; }
			public string status { get; set; }
			public string uuid { get; set; }

			// ---

			public class DocumentSection {

				public string external_id { get; set; }
				public string uuid { get; set; }
			}
		}
	}
}
