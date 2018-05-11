using Goldmint.Common;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
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
using Goldmint.CoreLogic.Services.RuntimeConfig;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private readonly IHostingEnvironment _environment;
		private readonly IConfiguration _configuration;
		private readonly AppConfig _appConfig;
		private readonly LogFactory _loggerFactory;
		private readonly RuntimeConfigHolder _runtimeConfigHolder;

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
					.AddJsonFile($"appsettings.{_environment.EnvironmentName}.Private.json", optional: true)
					.Build()
				;
				
				_appConfig = new AppConfig();
				_configuration.Bind(_appConfig);

				var nlogConfiguration = new NLog.Config.XmlLoggingConfiguration($"nlog.{_environment.EnvironmentName}.config");
				_loggerFactory = new LogFactory(nlogConfiguration);
				LogManager.Configuration = nlogConfiguration;
			} catch (Exception e) {
				throw new Exception("Failed to get app settings", e);
			}

			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("Launched");

			// runtime config
			_runtimeConfigHolder = new RuntimeConfigHolder(_loggerFactory);

			// custom db connection
			var dbCustomConnection = Environment.GetEnvironmentVariable("ASPNETCORE_DBCONNECTION");
			if (!string.IsNullOrWhiteSpace(dbCustomConnection)) {
				_appConfig.ConnectionStrings.Default = dbCustomConnection;
				logger.Info($"Using custom db connection: {dbCustomConnection}");
			}
		}

		public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime, IRuntimeConfigLoader runtimeConfigLoader) {

			applicationLifetime.ApplicationStopping.Register(OnServerStopRequested);
			applicationLifetime.ApplicationStopped.Register(OnServerStopped);

			// config loader
			_runtimeConfigHolder.SetLoader(runtimeConfigLoader);

			// setup ms logger
			app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().AddNLog();

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
					context.RequestServices?.GetService<LogFactory>()?.GetLogger(this.GetType().FullName)?.Error(error?.Error ?? new Exception("No extra data"), "Service failure");
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
					opts.SwaggerEndpoint("/" + ((_appConfig.Apps.RelativeApiPath).Trim('/') + "/swagger/api/swagger.json").Trim('/'), "API");
				});
			}

			// 404: redirect to index: not found, not a file, not api request
			app.Use(async (context, next) => {
				await next();
				if (context.Response.StatusCode == 404) {
					var resp = APIResponse.BadRequest(APIErrorCode.MethodNotFound);
					await resp.WriteResponse(context).ConfigureAwait(false);
				}
			});

			app.UseAuthentication();

			app.UseCors(opts => {
					opts.WithMethods("GET", "POST", "OPTIONS");
					opts.AllowAnyHeader();
					opts.AllowAnyOrigin();
				}
			);

			app.UseMvc();

			RunServices();
		}

		public void OnServerStopRequested() {
			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("Webserver stop requested");
		}

		public void OnServerStopped() {
			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("Webserver stopped");

			StopServices();
		}
	}
}
