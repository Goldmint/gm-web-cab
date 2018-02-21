using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Goldmint.Common;
using Microsoft.AspNetCore.Http;

namespace Goldmint.CoreLogic.Services.SignedDoc {

	public interface IDocSigningProvider {

		Task<bool> SendPrimaryAgreementRequest(string refId, string name, string email, DateTime date);
		Task<CallbackResult> OnServiceCallback(HttpRequest content);
	}
}
