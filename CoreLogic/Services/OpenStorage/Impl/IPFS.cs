using Goldmint.Common;
using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.OpenStorage.Impl {

	public class IPFS : IOpenStorageProvider {

		private string BaseUrl;
		private ILogger Logger;

		public IPFS(string baseUrl, LogFactory logFactory) {
			BaseUrl = baseUrl.Trim('/');
			Logger = logFactory.GetLoggerFor(this);
		}

		public async Task<string> UploadFile(Stream fileStream, string filename) {

			var url = BaseUrl + "/add";
			var dict = new Dictionary<string, string>();

			using (var req = new Request(Logger)) {
				await req
					.AcceptJson()
					//.BodyJsonRpc($"services.goldrate.{currency.ToString().ToLower()}", null)
					.OnResult(async (res) => {
						if (res.GetHttpStatus() == System.Net.HttpStatusCode.OK) {
							await res.ToJson(dict);
						}
					})
					.SendPost(url, TimeSpan.FromSeconds(120))
				;
			}

			if (dict.ContainsKey("Hash") && dict.ContainsKey("Size")) {
				return dict["Hash"];
			}

			return null;
		}
	}
}
