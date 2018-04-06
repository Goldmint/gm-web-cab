using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain;
using Goldmint.CoreLogic.Services.KYC;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Mutex;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.OpenStorage;
using Goldmint.CoreLogic.Services.Rate;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.CoreLogic.Services.Ticket;
using Goldmint.WebApplication.Models;
using Goldmint.WebApplication.Services.Cache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1 {

	public abstract class BaseController : Controller {

		protected AppConfig AppConfig { get; private set; }
		protected IHostingEnvironment HostingEnvironment { get; private set; }
		protected ILogger Logger { get; private set; }
		protected DAL.ApplicationDbContext DbContext { get; private set; }
		protected IMutexHolder MutexHolder { get; private set; }
		protected SignInManager<DAL.Models.Identity.User> SignInManager { get; private set; }
		protected UserManager<DAL.Models.Identity.User> UserManager { get; private set; }
		protected IKycProvider KycExternalProvider { get; private set; }
		protected INotificationQueue EmailQueue { get; private set; }
		protected ITemplateProvider TemplateProvider { get; private set; }
		protected ITicketDesk TicketDesk { get; private set; }
		protected IEthereumReader EthereumObserver { get; private set; }
		protected IGoldRateProvider GoldRateProvider { get; private set; }
		protected CachedGoldRate GoldRateCached { get; private set; }
		protected IOpenStorageProvider OpenStorageProvider { get; private set; }
		protected IDocSigningProvider DocSigningProvider { get; private set; }
		protected ICryptoassetRateProvider CryptoassetRateProvider { get; private set; }

		protected BaseController() { }

		[NonAction]
		private void InitServices(IServiceProvider services) {
			Logger = services.GetLoggerFor(this.GetType());
			AppConfig = services.GetRequiredService<AppConfig>();
			HostingEnvironment = services.GetRequiredService<IHostingEnvironment>();
			DbContext = services.GetRequiredService<DAL.ApplicationDbContext>();
			MutexHolder = services.GetRequiredService<IMutexHolder>();
			SignInManager = services.GetRequiredService<SignInManager<DAL.Models.Identity.User>>();
			UserManager = services.GetRequiredService<UserManager<DAL.Models.Identity.User>>();
			KycExternalProvider = services.GetRequiredService<IKycProvider>();
			EmailQueue = services.GetRequiredService<INotificationQueue>();
			TemplateProvider = services.GetRequiredService<ITemplateProvider>();
			TicketDesk = services.GetRequiredService<ITicketDesk>();
			EthereumObserver = services.GetRequiredService<IEthereumReader>();
			GoldRateProvider = services.GetRequiredService<IGoldRateProvider>();
			GoldRateCached = services.GetRequiredService<CachedGoldRate>();
			OpenStorageProvider = services.GetRequiredService<IOpenStorageProvider>();
			DocSigningProvider = services.GetRequiredService<IDocSigningProvider>();
			CryptoassetRateProvider = services.GetRequiredService<ICryptoassetRateProvider>();
		}

		// ---

		[NonAction]
		public override void OnActionExecuted(ActionExecutedContext context) {
			InitServices(context?.HttpContext?.RequestServices);
			base.OnActionExecuted(context);
		}

		[NonAction]
		public override void OnActionExecuting(ActionExecutingContext context) {
			base.OnActionExecuting(context);
		}

		[NonAction]
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			InitServices(context?.HttpContext?.RequestServices);
			await base.OnActionExecutionAsync(context, next);
		}

		// ---

		[NonAction]
		public string MakeAppLink(JwtAudience audience, string fragment) {

			var appUri = (string) null;
			if (audience == JwtAudience.Cabinet) {
				appUri = AppConfig.Apps.Cabinet.Url;
			}
			else if (audience == JwtAudience.Dashboard) {
				appUri = AppConfig.Apps.Dashboard.Url;
			}
			else {
				throw new NotImplementedException("Audience is not implemented. Could not create app link");
			}

			var uri = new UriBuilder(new Uri(appUri));
			if (fragment != null) {
				uri.Fragment = fragment;
			}
			return uri.ToString();
		}

		[NonAction]
		protected bool IsUserAuthenticated() {
			return HttpContext.User?.Identity.IsAuthenticated ?? false;
		}

		[NonAction]
		protected JwtAudience? GetCurrentAudience() {
			var audStr = HttpContext.User.Claims.FirstOrDefault(_ => _.Type == "aud")?.Value;
			if (audStr != null) {
				if (Enum.TryParse(audStr, true, out JwtAudience aud)) {
					return aud;
				}
			}
			return null;
		}

		[NonAction]
		protected long GetCurrentRights() {
			var rightsStr = HttpContext.User.Claims.FirstOrDefault(_ => _.Type == Core.Tokens.JWT.GMRightsField)?.Value;
			if (rightsStr != null) {
				if (long.TryParse(rightsStr, out long rights)) {
					return rights;
				}
			}
			return 0;
		}

		[NonAction]
		protected async Task<DAL.Models.Identity.User> GetUserFromDb() {
			if (IsUserAuthenticated()) {
				var name = UserManager.NormalizeKey(HttpContext.User.Identity.Name);
				return await DbContext.Users
					.Include(_ => _.UserOptions).ThenInclude(_ => _.DPADocument)
					.Include(_ => _.UserVerification).ThenInclude(_ => _.LastKycTicket)
					.Include(_ => _.UserVerification).ThenInclude(_ => _.LastAgreement)
					.AsTracking()
					.FirstAsync(user => user.NormalizedUserName == name)
				;
			}
			return null;
		}

		[NonAction]
		protected UserAgentInfo GetUserAgentInfo() {

			var ip = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

#if DEBUG
			if (HostingEnvironment.IsDevelopment() && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEBUG_CUSTOM_IP"))) {
				if (System.Net.IPAddress.TryParse(Environment.GetEnvironmentVariable("DEBUG_CUSTOM_IP"), out System.Net.IPAddress customIp)) {
					ip = customIp.MapToIPv4().ToString();
				}
			}
#endif

			// ip object
			var ipObj = System.Net.IPAddress.Parse(ip);

			// agent
			var agent = "Unknown";
			if (HttpContext.Request.Headers.TryGetValue("User-Agent", out var agentParsed)) {
				agent = agentParsed.ToString();
			}

			return new UserAgentInfo() {
				Ip = ip,
				IpObject = ipObj,
				Agent = agent,
			};
		}

		[NonAction]
		protected Locale GetUserLocale() {
			if (
				HttpContext.Request.Headers.TryGetValue("GM-LOCALE", out var localeHeader) && 
				!string.IsNullOrWhiteSpace(localeHeader.ToString()) && 
				Enum.TryParse(localeHeader.ToString(), true, out Locale localeEnum)
			) {
				return localeEnum;
			}
			return Locale.En;
		}
	}
}
