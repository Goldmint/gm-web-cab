using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Goldmint.Common.WebRequest {

	public sealed class Result : IDisposable {

		private bool _disposed;
		private HttpResponseMessage _responseRef;
		private HttpContent _contentRef;
		private HttpContent _content;

		public Result(HttpResponseMessage response) {
			_responseRef = response;
			_content = response.Content;
		}

		public Result(HttpContent content) {
			_contentRef = content;
			_content = content;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (_responseRef != null) _responseRef.Dispose();
				if (_contentRef != null) _contentRef.Dispose();
			}
			_disposed = true;
		}

		// ---

		private void AssertDisposed() {
			if (_disposed) {
				throw new Exception("Oups, disposed");
			}
		}

		public HttpStatusCode? GetHttpStatus() {
			AssertDisposed();
			return _responseRef?.StatusCode;
		}

		public async Task<string> ToRawString() {
			AssertDisposed();

			return await _content.ReadAsStringAsync();
		}

	}
}
