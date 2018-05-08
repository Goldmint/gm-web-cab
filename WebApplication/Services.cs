using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.Blockchain.Impl;
using Goldmint.CoreLogic.Services.Bus.Subscriber;
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
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.CoreLogic.Services.SignedDoc.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.CoreLogic.Services.Ticket.Impl;
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
using System;
using System.Linq;
using Goldmint.CoreLogic.Services.Rate;

namespace Goldmint.WebApplication {

	public partial class Startup {

		private DefaultSubscriber<CoreLogic.Services.Bus.Proto.SafeRatesMessage> _busSafeRatesSubscriber;
		private BusSafeRatesSource _busSafeRatesSource;

		public IServiceProvider ConfigureServices(IServiceCollection services) {

			// app config
			services.AddSingleton(_environment);
			services.AddSingleton(_configuration);
			services.AddSingleton(_appConfig);

			// logger
			services.AddSingleton(_loggerFactory);

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
				.AddUserManager<Core.UserAccount.GMUserManager>()
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
			services.AddScoped<ITicketDesk, DBTicketDesk>();

			// notifications
			services.AddScoped<INotificationQueue, DBNotificationQueue>();

			// templates
			services.AddSingleton<ITemplateProvider, TemplateProvider>();

			// kyc
			services.AddScoped<IKycProvider>(fac => {
				return new ShuftiProKycProvider(opts => {
					opts.ClientId = _appConfig.Services.ShuftiPro.ClientId;
					opts.ClientSecret = _appConfig.Services.ShuftiPro.ClientSecret;
				}, _loggerFactory);
			});

			// ethereum reader
			services.AddSingleton<IEthereumReader, EthereumReader>();

			// rates
			_busSafeRatesSubscriber = new DefaultSubscriber<CoreLogic.Services.Bus.Proto.SafeRatesMessage>(
				CoreLogic.Services.Bus.Proto.Topic.FiatRates,
				new Uri(_appConfig.Bus.WorkerRates.PubUrl),
				_loggerFactory
			);
			_busSafeRatesSubscriber.Run();
			_busSafeRatesSource = new BusSafeRatesSource(_busSafeRatesSubscriber, _loggerFactory);
			services.AddSingleton<IAggregatedSafeRatesSource>(_busSafeRatesSource);
			services.AddSingleton<SafeRatesFiatAdapter>();

			// open storage
			services.AddSingleton<IOpenStorageProvider>(fac => 
				new IPFS(_appConfig.Services.Ipfs.Url, _loggerFactory)
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
					logFactory: _loggerFactory
				);
				foreach (var t in _appConfig.Services.SignRequest.Templates) {
					if (Enum.TryParse(t.Locale, true, out Common.Locale locale)) {
						srv.AddTemplate(locale, t.Name, t.Filename, t.Template);
					}
				}
				return srv;
			});

			return services.BuildServiceProvider();
		}

		public void StopServices() {
			var logger = _loggerFactory.GetCurrentClassLogger();
			logger.Info("Stop services");

			_busSafeRatesSubscriber?.Dispose();
			NetMQ.NetMQConfig.Cleanup(true);
		}
	}
}
