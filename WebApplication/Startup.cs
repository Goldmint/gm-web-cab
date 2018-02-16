using Goldmint.Common;
using Goldmint.WebApplication.Core.Response;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private IHostingEnvironment _environment;
		private IConfiguration _configuration;
		private AppConfig _appConfig;
		private LogFactory _loggerFactory;
		private NLog.Config.XmlLoggingConfiguration _nlogConfiguration;

		// ---

		public Startup(IHostingEnvironment env, IConfiguration configuration) {

			_environment = env;
			_configuration = configuration;

			// config
			try {
				var cfgDir = Environment.GetEnvironmentVariable("ASPNETCORE_CONFIGPATH");
		
				_configuration = new ConfigurationBuilder()
					.SetBasePath(cfgDir)
					.AddJsonFile("appsettings.json", optional: false)
					.AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", optional: false)
					.Build()
				;
				
				_appConfig = new AppConfig();
				_configuration.Bind(_appConfig);

				_nlogConfiguration = new NLog.Config.XmlLoggingConfiguration($"nlog.{_environment.EnvironmentName}.config");
				_loggerFactory = new LogFactory(_nlogConfiguration);
			} catch (Exception e) {
				throw new Exception("Failed to get app settings", e);
			}

			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("Launched");

			// custom db connection
			var dbCustomConnection = Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION");
			if (!string.IsNullOrWhiteSpace(dbCustomConnection)) {
				_appConfig.ConnectionStrings.Default = dbCustomConnection;
				logger.Info($"Using custom db connection: {dbCustomConnection}");
			}
		}

		public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime) {

			// setup ms logger
			app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
				.AddNLog()
				.ConfigureNLog(_nlogConfiguration)
			;

			// nginx proxy
			{
				var forwardingOptions = new ForwardedHeadersOptions() {
					ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
					RequireHeaderSymmetry = false,
					ForwardLimit = null,
				};
				forwardingOptions.KnownNetworks.Clear();
				forwardingOptions.KnownProxies.Clear();
				app.UseForwardedHeaders(forwardingOptions);
			}

			// 503: response on exception
			app.UseExceptionHandler(builder => {
				builder.Run(async context => {
					var error = context.Features.Get<IExceptionHandlerFeature>();
					var resp = APIResponse.GeneralInternalFailure(error?.Error, !_environment.IsProduction());
					await resp.WriteResponse(context).ConfigureAwait(false);
				});
			});
			
			// 403: always write body if unathorized
			app.Use(async (context, next) => {
				await next();
				if (context.Response.StatusCode == 403) {
					var resp = APIResponse.BadRequest(APIErrorCode.Unauthorized);
					await resp.WriteResponse(context).ConfigureAwait(false);
				}
			});
			
			// check content type
			app.Use(async (context, next) => {
				var flatPath = context.Request.Path.ToString();

				if (context.Request.Method == "POST" &&  flatPath.StartsWith("/api/") && !flatPath.Contains("/callback/")) {
					if (!(context.Request.ContentType?.StartsWith("application/json") ?? false)) {
						var resp = APIResponse.BadRequest(APIErrorCode.InvalidContentType, "Json format is only allowed");
						await resp.WriteResponse(context).ConfigureAwait(false);
						return;
					}
				}
				await next();
			});

			// swagger
			if (!_environment.IsProduction()) {
				app.UseSwagger(opts => {
				});
				app.UseSwaggerUI(opts => {
					opts.SwaggerEndpoint("/swagger/api/swagger.json", "API v1");
				});
			}

			app.UseDefaultFiles();
			app.UseStaticFiles();

			// redirect to index: not found, not a file, not api request
			app.Use(async (context, next) => {
				await next();
				if (context.Response.StatusCode == 404 && !System.IO.Path.HasExtension(context.Request.Path.Value) && !context.Request.Path.Value.ToLower().StartsWith("/api/")) {
					context.Request.Path = "/index.html";
					await next();
				}
			});

			app.UseAuthentication();

			app.UseCors(opts => opts
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod()
			);

			app.UseMvc();
		}
	}
}
