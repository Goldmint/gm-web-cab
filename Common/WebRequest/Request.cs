using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Goldmint.Common.WebRequest {

	public sealed partial class Request : IDisposable {

		private static readonly TimeSpan Timeout10 = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan Timeout30 = TimeSpan.FromSeconds(30);

		private HttpContent _body;
		private string _query;
		private AuthenticationHeaderValue _auth;
		private List<MediaTypeWithQualityHeaderValue> _hdrAccept;
		private Func<Result, Task> _callbackFnc;
		private Action<Result> _callbackAct;
		private ILogger _logger;

		public Request(ILogger logger) {
			_logger = logger;
			_hdrAccept = new List<MediaTypeWithQualityHeaderValue>();
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				_body?.Dispose();
			}
		}

		// ---

		public Request Query(Parameters query) {
			_query = query?.ToUrlEncoded();
			return this;
		}

		public Request Query(string query) {
			_query = query;
			return this;
		}

		public Request AuthBasic(string auth, bool convertToBase64) {
			if (convertToBase64) {
				auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
			}
			_auth = new AuthenticationHeaderValue("Basic", auth);
			return this;
		}

		public Request AuthToken(string token) {
			_auth = new AuthenticationHeaderValue("Token", token);
			return this;
		}

		public Request Accept(MediaTypeWithQualityHeaderValue accept) {
			_hdrAccept.Add(accept);
			return this;
		}

		public Request Body(HttpContent body) {
			_body = body;
			return this;
		}

		public Request OnResult(Func<Result, Task> cbk) {
			_callbackFnc = cbk;
			_callbackAct = null;
			return this;
		}

		public Request OnResult(Action<Result> cbk) {
			_callbackAct = cbk;
			_callbackFnc = null;
			return this;
		}

		// ---

		public async Task<bool> SendGet(string url, TimeSpan? timeout = null) {
			return await Send(false, url, timeout ?? Timeout10);
		}

		public async Task<bool> SendPost(string url, TimeSpan? timeout = null) {
			return await Send(true, url, timeout ?? Timeout30);
		}

		private async Task<bool> Send(bool post, string url, TimeSpan timeout) {

			var urlb = new UriBuilder(url);
			urlb.Query = _query;
			url = urlb.ToString();

			using (var client = new HttpClient()) {

				client.Timeout = timeout;
				if (_auth != null) {
					client.DefaultRequestHeaders.Authorization = _auth;
				}

				foreach (var ah in _hdrAccept) {
					client.DefaultRequestHeaders.Accept.Add(ah);
				}

				try {
					_logger?.Trace($"Sending request to `{url}`");

					if (post) {
						using (var res = new Result(await client.PostAsync(url, _body))) {
							if (_callbackFnc != null) await _callbackFnc(res);
							_callbackAct?.Invoke(res);
						}
					}
					else {
						using (var res = new Result(await client.GetAsync(url))) {
							if (_callbackFnc != null) await _callbackFnc(res);
							_callbackAct?.Invoke(res);
						}
					}
				} catch (Exception e) {
					_logger?.Error(e, "Failed to send request to " + url);
				}
			}

			return false;
		}
	}
}
