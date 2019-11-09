using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl;
using Goldmint.CoreLogic.Services.KYC;
using Goldmint.CoreLogic.Services.KYC.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Price;
using Goldmint.CoreLogic.Services.Price.Impl;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Services.OAuth.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private PriceDispatcher _priceDispatcher;
		private RuntimeConfigUpdater _configUpdater;

		public IServiceProvider ConfigureServices(IServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton<Serilog.ILogger>(Log.Logger);
			services.AddLogging(builder => {
				builder
					.AddFilter("Microsoft", LogLevel.Error)
					.AddFilter("System", LogLevel.Error)
					.AddConsole()
				;
			});

			// swagger
			if (!_environment.IsProduction()) {
				services.AddSwaggerGen(opts => {
					opts.SwaggerDoc("api", new Swashbuckle.AspNetCore.Swagger.Info() {
						Title = "API",
						Version = "latest",
					});
					opts.CustomSchemaIds((type) => type.FullName);
					opts.IncludeXmlComments(System.IO.Path.Combine(Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationBasePath, "Goldmint.WebApplication.xml"));
					opts.OperationFilter<Core.Swagger.JWTHeaderParameter>();
					opts.OperationFilter<Core.Swagger.DefaultErrorResponse>();
					opts.DocumentFilter<Core.Swagger.EnumDescription>();
					//opts.TagActionsBy(Core.Swagger.TagSelector);
				});
			}

			// db
			services.AddDbContext<ApplicationDbContext>(opts => {
				opts.UseMySql(_appConfig.ConnectionStrings.Default, myopts => {
					myopts.UseRelationalNulls(true);
				});
			});

            // runtime config
            services.AddSingleton(_runtimeConfigHolder);
			services.AddSingleton<IRuntimeConfigLoader, DbRuntimeConfigLoader>();

			// identity
			var idbld = services
				.AddIdentityCore<User>(opts => {
					opts.SignIn.RequireConfirmedEmail = false;
					opts.User.RequireUniqueEmail = true;

					opts.Password.RequireDigit = false;
					opts.Password.RequiredLength = ValidationRules.PasswordMinLength;
					opts.Password.RequireNonAlphanumeric = false;
					opts.Password.RequireUppercase = false;
					opts.Password.RequireLowercase = false;
					opts.Password.RequiredUniqueChars = 1;

					opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
					opts.Lockout.MaxFailedAccessAttempts = 5;
					opts.Lockout.AllowedForNewUsers = true;
				})
			;
			idbld = new IdentityBuilder(idbld.UserType, typeof(Role), services);
			idbld
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddSignInManager<SignInManager<User>>()
				.AddUserManager<Core.UserAccount.GmUserManager>()
				.AddDefaultTokenProviders()
			;

			// auth
			services
				.AddAuthentication(opts => {
					opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
					opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
					opts.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
					opts.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(opts => {
					opts.RequireHttpsMetadata = _environment.IsProduction();
					opts.SaveToken = false;
					opts.Events = Core.Tokens.JWT.AddEvents();
					opts.TokenValidationParameters = Core.Tokens.JWT.ValidationParameters(_appConfig);
				})
			;

			// authorization
			services.AddAuthorization(opts => {

				// jwt audience
				foreach (var v in (JwtAudience[]) Enum.GetValues(typeof(JwtAudience))) {
					var audSett = _appConfig.Auth.Jwt.Audiences.FirstOrDefault(_ => _.Audience == v.ToString());
					if (audSett != null) {
						opts.AddPolicy(
							Core.Policies.Policy.JWTAudienceTemplate + v.ToString(),
							policy => policy.AddRequirements(new Core.Policies.RequireJWTAudience(v))
						);
					}
				}

				// jwt area
				foreach (var v in (JwtArea[]) Enum.GetValues(typeof(JwtArea))) {
					opts.AddPolicy(
						Core.Policies.Policy.JWTAreaTemplate + v.ToString(),
						policy => policy.AddRequirements(new Core.Policies.RequireJWTArea(v))
					);
				}
			});
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireJWTAudience.Handler>();
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireJWTArea.Handler>();
			services.AddScoped<GoogleProvider>();

			// tokens
			services.Configure<DataProtectionTokenProviderOptions>(opts => {
				opts.Name = "Default";
				opts.TokenLifespan = TimeSpan.FromHours(24);
			});

			// mvc
			services
				.AddMvc(opts => {
					opts.RespectBrowserAcceptHeader = false;
					opts.ReturnHttpNotAcceptable = false;
					opts.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.XmlSerializerInputFormatter>();
					opts.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.XmlSerializerOutputFormatter>();
				})
				.AddJsonOptions(options => {
					options.SerializerSettings.ContractResolver = Json.CamelCaseSettings.ContractResolver;
				})
			;
			services.AddCors();

			// http context
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			// notifications
			if (_environment.IsDevelopment()) {
				services.AddScoped<INotificationQueue, NullNotificationQueue>();
			}
			else {
				services.AddScoped<INotificationQueue, DBNotificationQueue>();
			}

			// templates
			services.AddSingleton<ITemplateProvider, TemplateProvider>();

			// kyc
			if (_environment.IsProduction()) {
				services.AddScoped<IKycProvider>(fac => {
					return new ShuftiPro13KycProvider(opts => {
						opts.ClientId = _appConfig.Services.ShuftiPro.ClientId;
						opts.ClientSecret = _appConfig.Services.ShuftiPro.ClientSecret;
					}, Log.Logger);
				});
			}
			else {
				services.AddScoped<IKycProvider, DebugKycProvider>();
			}

			// ethereum reader
			services.AddSingleton<IEthereumReader, EthereumReader>();

			// nats
			var natsConnPool = new CoreLogic.Services.Bus.Impl.Bus(_appConfig, Log.Logger);
			services.AddSingleton<CoreLogic.Services.Bus.IBus>(natsConnPool);

			// prices
			{
				var gmPrices = new GMPriceProvider(
					opts => {
						opts.GoldUrl = _appConfig.Services.GMRatesProvider.GoldRateUrl;
						opts.EthUrl = _appConfig.Services.GMRatesProvider.EthRateUrl;
					},
					Log.Logger
				);

				_priceDispatcher = new PriceDispatcher(
					gmPrices, gmPrices, _runtimeConfigHolder, Log.Logger, 
					opts => {
						opts.PriceRequestPeriod = TimeSpan.FromSeconds(60);
						opts.PriceRequestTimeout = TimeSpan.FromSeconds(10);
					}
				);
				services.AddSingleton<IPriceSource>(_priceDispatcher);
			}

			// runtime config updater
			_configUpdater = new RuntimeConfigUpdater(natsConnPool, _runtimeConfigHolder, Log.Logger);
			services.AddSingleton<IRuntimeConfigUpdater>(_configUpdater);

			return services.BuildServiceProvider();
		}

		public void RunServices() {
			Log.Logger.Information("Run services");
			_runtimeConfigHolder.Reload().Wait();
			_priceDispatcher?.Start();
			_configUpdater?.Run();
		}

		public void StopServices() {
			Log.Logger.Information("Stop services");

			try {
				_priceDispatcher?.Stop();
				_priceDispatcher?.Dispose();
				_configUpdater?.Stop();
				_configUpdater?.Dispose();
			} catch (Exception e) {
				Log.Logger.Error(e, "Failed to stop services");
			}

			Log.Logger.Information("Services stopped");
		}
	}
}
