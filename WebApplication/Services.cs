using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Blockchain.Impl;
using Goldmint.CoreLogic.Services.KYC;
using Goldmint.CoreLogic.Services.KYC.Impl;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Localization.Impl;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.CoreLogic.Services.OpenStorage;
using Goldmint.CoreLogic.Services.OpenStorage.Impl;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.RuntimeConfig;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.CoreLogic.Services.SignedDoc.Impl;
using Goldmint.CoreLogic.Services.The1StPayments;
using Goldmint.CoreLogic.Services.Oplog;
using Goldmint.CoreLogic.Services.Oplog.Impl;
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
using System.Threading.Tasks;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private CoreLogic.Services.Bus.Subscriber.CentralSubscriber _busCentralSubscriber;
		private CoreLogic.Services.Bus.Publisher.ChildPublisher _busChildPublisher;
		private CoreLogic.Services.Rate.Impl.BusSafeRatesSource _busSafeRatesSource;
		private CoreLogic.Services.Bus.Telemetry.ApiTelemetryAccumulator _apiTelemetryAccumulator;
		private Services.Bus.AggregatedTelemetryHolder _aggregatedTelemetryHolder;

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

					opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
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
			if (_environment.IsProduction()) {
				services.AddScoped<IKycProvider>(fac => {
					return new ShuftiProKycProvider(opts => {
						opts.ClientId = _appConfig.Services.ShuftiPro.ClientId;
						opts.ClientSecret = _appConfig.Services.ShuftiPro.ClientSecret;
					}, LogManager.LogFactory);
				});
			}
			else {
				services.AddScoped<IKycProvider, DebugKycProvider>();
			}

			// cc payment acquirer
			services.AddScoped<The1StPayments>(fac => {
				return new The1StPayments(opts => {
					opts.MerchantGuid = _appConfig.Services.The1StPayments.MerchantGuid;
					opts.ProcessingPassword = _appConfig.Services.The1StPayments.ProcessingPassword;
					opts.Gateway = _appConfig.Services.The1StPayments.Gateway;
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

			// rates
			_busSafeRatesSource = new CoreLogic.Services.Rate.Impl.BusSafeRatesSource(_runtimeConfigHolder, LogManager.LogFactory);
			services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSource);
			services.AddSingleton<CoreLogic.Services.Rate.Impl.SafeRatesFiatAdapter>();

			// aggregated telemetry from centra pub
			_aggregatedTelemetryHolder = new Services.Bus.AggregatedTelemetryHolder(_appConfig);
			services.AddSingleton(_aggregatedTelemetryHolder);

			// subscribe to central pub
			_busCentralSubscriber = new CoreLogic.Services.Bus.Subscriber.CentralSubscriber(
				new [] {
					CoreLogic.Services.Bus.Proto.Topic.FiatRates,
					CoreLogic.Services.Bus.Proto.Topic.AggregatedTelemetry,
					CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated,
				},
				new Uri(_appConfig.Bus.CentralPub.Endpoint),
				LogManager.LogFactory
			);
			_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.FiatRates, (p, s) => {
				_busSafeRatesSource.OnNewRates(p, s);
				_apiTelemetryAccumulator.AccessData(_ => _.RatesData = p as CoreLogic.Services.Bus.Proto.SafeRates.SafeRatesMessage);
			});
			_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.AggregatedTelemetry, (p, s) => {
				_aggregatedTelemetryHolder.OnUpdate(p, s);
			});
			_busCentralSubscriber.SetTopicCallback(CoreLogic.Services.Bus.Proto.Topic.ConfigUpdated, (p, s) => {
				Task.Factory.StartNew(async () => {
					await _runtimeConfigHolder.Reload();
					var rcfg = _runtimeConfigHolder.Clone();
					_apiTelemetryAccumulator.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);
				});
			});

			// open storage
			services.AddSingleton<IOpenStorageProvider>(fac => 
				new IPFS(_appConfig.Services.Ipfs.Url, LogManager.LogFactory)
			);

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

			// custom child-pub port
			var busChildPubCustomPort = Environment.GetEnvironmentVariable("ASPNETCORE_BUS_CHD_PORT");
			if (!string.IsNullOrWhiteSpace(busChildPubCustomPort)) {
				_appConfig.Bus.ChildPub.PubPort = int.Parse(busChildPubCustomPort);
			}

			// launch child pub
			_busChildPublisher = new CoreLogic.Services.Bus.Publisher.ChildPublisher(_appConfig.Bus.ChildPub.PubPort, LogManager.LogFactory);
			services.AddSingleton(_busChildPublisher);

			// telemetry accum/pub
			_apiTelemetryAccumulator = new CoreLogic.Services.Bus.Telemetry.ApiTelemetryAccumulator(
				_busChildPublisher,
				TimeSpan.FromSeconds(_appConfig.Bus.ChildPub.PubTelemetryPeriodSec),
				LogManager.LogFactory
			);
			services.AddSingleton(_apiTelemetryAccumulator);

			return services.BuildServiceProvider();
		}

		public void RunServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Run services");

			_runtimeConfigHolder.Reload().Wait();
			var rcfg = _runtimeConfigHolder.Clone();
			_apiTelemetryAccumulator?.AccessData(_ => _.RuntimeConfigStamp = rcfg.Stamp);

			_busCentralSubscriber?.Run();
			_busChildPublisher?.Run();
			_apiTelemetryAccumulator?.Run();
		}

		public void StopServices() {
			var logger = LogManager.LogFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			try {
				_apiTelemetryAccumulator?.StopAsync();
				_busChildPublisher?.StopAsync();
				_busCentralSubscriber?.StopAsync();

				_apiTelemetryAccumulator?.Dispose();
				_busChildPublisher?.Dispose();
				_busCentralSubscriber?.Dispose();

				_busSafeRatesSource?.Dispose();
				_aggregatedTelemetryHolder?.Dispose();

				NetMQ.NetMQConfig.Cleanup(true);
			}
			catch (Exception e) {
				logger.Error(e);
			}

			logger.Info("Services stopped");
		}
	}
}
