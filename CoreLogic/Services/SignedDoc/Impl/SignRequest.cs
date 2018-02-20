using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.Common.WebRequest;
using Microsoft.AspNetCore.Http;
using NLog;

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

		public async Task<bool> SendAgreementRequest(string refId, string name, string email, DateTime date) {

			var template = _templates.FirstOrDefault(_ => _.Name == SignedDocumentType.Agreement.ToString());
			if (template == null) {
				_logger.Error($"Template not found for {nameof(SendAgreementRequest)}");
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
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							success = true;
						}
					})
					.SendPost(url, TimeSpan.FromSeconds(120))
				;
			}

			return success;
		}

		public Task<CallbackResult> OnServiceCallback(HttpRequest content) {

			// TODO: check result and signature

			return Task.FromResult(new CallbackResult() {
				OverallStatus = OverallStatus.Failed
			});
		}

		// ---

		private class TemplateDesc {
			public string Name { get; set; }
			public string Filename { get; set; }
			public string TemplateUrl { get; set; }
		}
	}
}
