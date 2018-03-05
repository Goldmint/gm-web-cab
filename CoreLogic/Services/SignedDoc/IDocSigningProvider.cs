using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Goldmint.Common;
using Microsoft.AspNetCore.Http;

namespace Goldmint.CoreLogic.Services.SignedDoc {

	public interface IDocSigningProvider {

		Task<bool> SendDpaRequest(string refId, string firstName, string lastName, string email, DateTime date, string redirectUrl);
		Task<bool> SendPrimaryAgreementRequest(string refId, string firstName, string lastName, string email, DateTime date, string redirectUrl);
		Task<CallbackResult> OnServiceCallback(HttpRequest content);
	}
}
