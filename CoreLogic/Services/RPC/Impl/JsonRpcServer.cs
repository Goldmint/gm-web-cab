using Goldmint.Common;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.RPC.Impl {

	public class JsonRPCServer<TService> : IDisposable, IRPCServer where TService : AustinHarris.JsonRpc.JsonRpcService {

		private HttpListener _listener;
		private ILogger _logger;
		private TService _rpcService;
		private object _monitor;

		public JsonRPCServer(TService service, LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
			_listener = new HttpListener();
			_monitor = new object();
			_rpcService = service;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (_listener != null) {
					_listener.Close();
					_listener = null;
				}
			}
		}

		public void Start(string address) {
			if (_listener == null || _listener.IsListening) return;

			_listener.Prefixes.Add(address);

			_listener.Start();
			_listener.BeginGetContext(
				new AsyncCallback(ListenerCallback),
				this
			);
		}

		public void Stop() {
			if (_listener == null || !_listener.IsListening) return;
			lock (_monitor) {
				_listener.Stop();
			}
		}

		// ---

		private void ListenerCallback(IAsyncResult result) {

			var server = result.AsyncState as JsonRPCServer<TService>;

			lock (server._monitor) {

				var listener = server._listener;
				var logger = server._logger;

				var context = listener.EndGetContext(result);

				if (!listener.IsListening) {
					return;
				}

				try {
					var request = context.Request;
					var response = context.Response;
					var responseBytes = (byte[])null;

					response.ContentType = "application/json";
					response.ContentEncoding = Encoding.UTF8;

					try {
						if (request.HttpMethod == "POST") {

							using (var reader = new StreamReader(request.InputStream, request.ContentEncoding)) {
								var body = reader.ReadToEnd();
								var procTask = AustinHarris.JsonRpc.JsonRpcProcessor.Process(body, server._rpcService);

								response.StatusCode = (int)HttpStatusCode.OK;
								responseBytes = Encoding.UTF8.GetBytes(procTask.Result);
							}
						}
						else {
							throw new Exception();
						}
					}
					catch (Exception e) {
						response.StatusCode = (int)HttpStatusCode.BadRequest;
						responseBytes = Encoding.UTF8.GetBytes("{}");
						throw e;
					}
					finally {

						if (responseBytes != null) {
							try {
								response.ContentLength64 = responseBytes.Length;
								response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
							}
							catch { }
						}

						response.OutputStream.Close();
					}
				}
				catch (Exception e) {
					logger.Error(e);
				}
				finally {
					listener.BeginGetContext(
						new AsyncCallback(ListenerCallback),
						server
					);
				}
			}
		}
	}
}
