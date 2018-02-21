using Goldmint.Common;
using Goldmint.CoreLogic.Services.Acquiring;
using Goldmint.CoreLogic.Services.Acquiring.Impl;
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
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.Rate.Impl;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.CoreLogic.Services.Ticket.Impl;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Services.Cache;
using Goldmint.WebApplication.Services.OAuth.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Goldmint.CoreLogic.Services.OpenStorage.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.CoreLogic.Services.SignedDoc.Impl;

namespace Goldmint.WebApplication {

	public partial class Startup {

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

				// area
				opts.AddPolicy(Core.Policies.Policy.AccessTFAArea, policy => policy.AddRequirements(new Core.Policies.RequireAreaToken(JwtArea.TFA)));
				opts.AddPolicy(Core.Policies.Policy.AccessAuthorizedArea, policy => policy.AddRequirements(new Core.Policies.RequireAreaToken(JwtArea.Authorized)));

				// access level
				foreach (var ar in Enum.GetValues(typeof(AccessRights)) as AccessRights[]) {
					opts.AddPolicy(Core.Policies.Policy.HasAccessRightsTemplate + ar.ToString(), policy => policy.AddRequirements(new Core.Policies.RequireAccessRights(ar)));
				}
			});
			services.AddSingleton<IAuthorizationHandler, Core.Policies.RequireAreaToken.Handler>();
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
			services.AddSingleton<ITicketDesk, DBTicketDesk>();

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

			// cc acquirer
			services.AddScoped<ICardAcquirer>(fac => {
				return new The1stPayments(opts => {
					opts.MerchantGuid = _appConfig.Services.The1stPayments.MerchantGuid;
					opts.ProcessingPassword = _appConfig.Services.The1stPayments.ProcessingPassword;
					opts.Gateway = _appConfig.Services.The1stPayments.Gateway;
				}, _loggerFactory);
			});

			// ethereum reader
			services.AddSingleton<IEthereumReader, InfuraReader>();

			// rates
			services.AddSingleton<CachedGoldRate>();
#if DEBUG
			services.AddSingleton<IGoldRateProvider>(fac => new DebugGoldRateProvider());
#else
			services.AddSingleton<IGoldRateProvider>(fac => new GoldRateRpcProvider(_appConfig.RpcServices.GoldRateUsdUrl, _loggerFactory));
#endif

			// open storage
			services.AddSingleton<IOpenStorageProvider>(fac => 
				new IPFS(_appConfig.Services.IPFS.Url, _loggerFactory)
			);

			// docs signing
			services.AddSingleton<IDocSigningProvider>(fac => {
				var srv = new SignRequest(
					baseUrl: _appConfig.Services.SignRequest.Url,
					authString: _appConfig.Services.SignRequest.Auth,
					senderEmail: _appConfig.Services.SignRequest.SenderEmail,
					senderEmailName: "GoldMint",
					logFactory: _loggerFactory
				);
				foreach (var t in _appConfig.Services.SignRequest.Templates) { 
					srv.AddTemplate(t.Name, t.Filename, t.Template);
				}
				return srv;
			});

			return services.BuildServiceProvider();
		}

	}
}
