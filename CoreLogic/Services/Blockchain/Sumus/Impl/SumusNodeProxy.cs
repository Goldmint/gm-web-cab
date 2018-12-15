using Goldmint.Common.WebRequest;
using NLog;
using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Blockchain.Sumus.Impl {

	public static class SumusNodeProxy {

		public static async Task<Tres> Get<Tres>(string url, ILogger logger) where Tres:class {
			Tres ret = null;
			bool ok = false;
			using (var req = new Request(logger)) {
				await req
					.AcceptJson()
					.OnResult(async (res) => {
						var des = await res.ToJson<JResponse<Tres>>();
						if (res.GetHttpStatus() != System.Net.HttpStatusCode.OK) {
							logger?.Error($"Sumus node proxy response status is {res.GetHttpStatus()} with message `{des.msg}`");
						} else {
							ret = des.res;
							ok = true;
						}
					})
					.SendGet(url, TimeSpan.FromSeconds(90))
				;
			}
			if (!ok) {
				throw new Exception("Failed to POST");
			}
			return ret;
		}

		public static async Task<Tres> Post<Tres, Treq>(string url, Treq body, ILogger logger) where Treq:class where Tres:class {
			Tres ret = null;
			bool ok = false;
			using (var req = new Request(logger)) {
				await req
					.AcceptJson()
					.BodyJson(body)
					.OnResult(async (res) => {
						var des = await res.ToJson<JResponse<Tres>>();
						if (res.GetHttpStatus() != System.Net.HttpStatusCode.OK) {
							logger?.Error($"Sumus node proxy response status is {res.GetHttpStatus()} with message `{des.msg}`");
						} else {
							ret = des.res;
							ok = true;
						}
					})
					.SendPost(url, TimeSpan.FromSeconds(90))
				;
			}
			if (!ok) {
				throw new Exception("Failed to POST");
			}
			return ret;
		}

		internal class JResponse<T> where T:class {
			public string msg { get;set; }
			public T res { get; set; }
		}
	}
}
