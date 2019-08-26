using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum.Impl;
using Goldmint.CoreLogic.Services.Google.Impl;
using Goldmint.CoreLogic.Services.KYC;
using Goldmint.CoreLogic.Services.KYC.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.Oplog.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.CoreLogic.Services.SignedDoc.Impl;
using Goldmint.CoreLogic.Services.The1StPayments;
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
using NLog;
using System;
using System.Linq;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private BusSafeRatesSource _busSafeRatesSource;
		private RuntimeConfigUpdater _configUpdater;

		public IServiceProvider ConfigureServices(IServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(LogManager.LogFactory);

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
					opts.TagActionsBy(Core.Swagger.TagSelector);
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
					opts.SignIn.RequireConfirmedEmail = true;
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

				// access rights
				foreach (var ar in (AccessRights[]) Enum.GetValues(typeof(AccessRights))) {
					opts.AddPolicy(
						Core.Policies.Policy.AccessRightsTemplate + ar.ToString(), 
						policy => policy.AddRequirements(new Core.Policies.RequireAccessRights(ar))
					);
				}
			});
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireJWTAudience.Handler>();
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireJWTArea.Handler>();
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireAccessRights.Handler>();
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

			// mutex
			services.AddScoped<IMutexHolder, DBMutexHolder>();

			// tickets desk
			services.AddScoped<IOplogProvider, DbOplogProvider>();

			// notifications
			services.AddScoped<INotificationQueue, DBNotificationQueue>();

			// templates
			services.AddSingleton<ITemplateProvider, TemplateProvider>();

			// kyc
			//if (_environment.IsProduction()) {
				services.AddScoped<IKycProvider>(fac => {
					return new ShuftiPro13KycProvider(opts => {
						opts.ClientId = _appConfig.Services.ShuftiPro.ClientId;
						opts.ClientSecret = _appConfig.Services.ShuftiPro.ClientSecret;
					}, LogManager.LogFactory);
				});
			//}
			//else {
			//	services.AddScoped<IKycProvider, DebugKycProvider>();
			//}

			// cc payment acquirer
			services.AddScoped<The1StPayments>(fac => {
				return new The1StPayments(opts => {
					opts.MerchantGuid = _appConfig.Services.The1StPayments.MerchantGuid;
					opts.ProcessingPassword = _appConfig.Services.The1StPayments.ProcessingPassword;
					opts.Gateway = _appConfig.Services.The1StPayments.Gateway;
					opts.RsInitStoreSms3D = _appConfig.Services.The1StPayments.RsInitStoreSms3D;
					opts.RsInitRecurrent3D = _appConfig.Services.The1StPayments.RsInitRecurrent3D;
					opts.RsInitStoreSms = _appConfig.Services.The1StPayments.RsInitStoreSms;
					opts.RsInitRecurrent = _appConfig.Services.The1StPayments.RsInitRecurrent;
					opts.RsInitStoreCrd = _appConfig.Services.The1StPayments.RsInitStoreCrd;
					opts.RsInitRecurrentCrd = _appConfig.Services.The1StPayments.RsInitRecurrentCrd;
					opts.RsInitStoreP2P = _appConfig.Services.The1StPayments.RsInitStoreP2P;
					opts.RsInitRecurrentP2P = _appConfig.Services.The1StPayments.RsInitRecurrentP2P;
				}, LogManager.LogFactory);
			});

			// ethereum reader
			services.AddSingleton<IEthereumReader, EthereumReader>();

			// nats factory
			var natsFactory = new NATS.Client.ConnectionFactory();

			// nats connection getter
			NATS.Client.IConnection natsConnGetter() {
				var opts = NATS.Client.ConnectionFactory.GetDefaultOptions();
				opts.Url = _appConfig.Bus.Nats.Endpoint;
				opts.AllowReconnect = true;
				return natsFactory.CreateConnection(opts);
			}
			services.AddScoped(_ => natsConnGetter());

			// rates
			_busSafeRatesSource = new CoreLogic.Services.Rate.Impl.BusSafeRatesSource(natsConnGetter(), _runtimeConfigHolder, LogManager.LogFactory);
			services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSource);
			services.AddSingleton<CoreLogic.Services.Rate.Impl.SafeRatesFiatAdapter>();

			// runtime config updater
			_configUpdater = new RuntimeConfigUpdater(natsConnGetter(), natsConnGetter(), _runtimeConfigHolder, LogManager.LogFactory);
			services.AddSingleton<IRuntimeConfigUpdater>(_configUpdater);

			// docs signing
			services.AddSingleton<IDocSigningProvider>(fac => {
				var srv = new SignRequest(
					opts: new SignRequest.Options() { 
						BaseUrl = _appConfig.Services.SignRequest.Url,
						AuthString = _appConfig.Services.SignRequest.Auth,
						SenderEmail = _appConfig.Services.SignRequest.SenderEmail,
						SenderEmailName = "GoldMint",
					},
					logFactory: LogManager.LogFactory
				);
				foreach (var t in _appConfig.Services.SignRequest.Templates) {
					if (Enum.TryParse(t.Locale, true, out Common.Locale locale)) {
						srv.AddTemplate(locale, t.Name, t.Filename, t.Template);
					}
				}
				return srv;
			});

			// google sheets
			if (_appConfig.Services.GoogleSheets != null) {
				services.AddSingleton(new Sheets(_appConfig, LogManager.LogFactory));
			}

			return services.BuildServiceProvider();
		}

		public void RunServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Run services");
			_runtimeConfigHolder.Reload().Wait();
			_busSafeRatesSource?.Run();
			_configUpdater?.Run();
		}

		public void StopServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			try {
				_busSafeRatesSource?.Stop();
				_busSafeRatesSource?.Dispose();
				_configUpdater?.Stop();
				_configUpdater?.Dispose();
			} catch (Exception e) {
				logger.Error(e);
			}

			logger.Info("Services stopped");
		}
	}
}
