﻿using Goldmint.Common;
using Goldmint.CoreLogic.Services.Blockchain.Ethereum;
using Goldmint.CoreLogic.Services.KYC;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification;
using Goldmint.CoreLogic.Services.Price.Impl;
using Goldmint.CoreLogic.Services.RuntimeConfig.Impl;
using Goldmint.WebApplication.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;
using Serilog;
using Goldmint.CoreLogic.Services.Price;
using Goldmint.CoreLogic.Services.Bus;

namespace Goldmint.WebApplication.Controllers.v1 {

	public abstract class BaseController : Controller {

		protected AppConfig AppConfig { get; private set; }
		protected IHostingEnvironment HostingEnvironment { get; private set; }
		protected ILogger Logger { get; private set; }
		protected DAL.ApplicationDbContext DbContext { get; private set; }
		protected SignInManager<DAL.Models.Identity.User> SignInManager { get; private set; }
		protected UserManager<DAL.Models.Identity.User> UserManager { get; private set; }
		protected IKycProvider KycExternalProvider { get; private set; }
		protected INotificationQueue EmailQueue { get; private set; }
		protected ITemplateProvider TemplateProvider { get; private set; }
		protected IEthereumReader EthereumObserver { get; private set; }
		protected IPriceSource PriceSource { get; private set; }
		protected RuntimeConfigHolder RuntimeConfigHolder { get; private set; }
		protected IBus Bus { get; private set; }

		protected BaseController() { }

		[NonAction]
		private void InitServices(IServiceProvider services) {
			Logger = services.GetLoggerFor(this.GetType());
			AppConfig = services.GetRequiredService<AppConfig>();
			HostingEnvironment = services.GetRequiredService<IHostingEnvironment>();
			DbContext = services.GetRequiredService<DAL.ApplicationDbContext>();
			SignInManager = services.GetRequiredService<SignInManager<DAL.Models.Identity.User>>();
			UserManager = services.GetRequiredService<UserManager<DAL.Models.Identity.User>>();
			KycExternalProvider = services.GetRequiredService<IKycProvider>();
			EmailQueue = services.GetRequiredService<INotificationQueue>();
			TemplateProvider = services.GetRequiredService<ITemplateProvider>();
			EthereumObserver = services.GetRequiredService<IEthereumReader>();
			PriceSource = services.GetRequiredService<IPriceSource>();
			RuntimeConfigHolder = services.GetRequiredService<RuntimeConfigHolder>();
			Bus = services.GetRequiredService<IBus>();
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
		public string MakeAppLink(JwtAudience audience, string fragment = null) {

			var origin = "";
			if (HttpContext.Request?.Headers?.TryGetValue("Origin", out var val) ?? false) {
				origin = val.ToString();
			}

			var appUri = "";
			if (audience == JwtAudience.Cabinet) {
				appUri = AppConfig.Apps.Cabinet.Url.FirstOrDefault();
				foreach (var u in AppConfig.Apps.Cabinet.Url) {
					if (u.IndexOf(origin) == 0) {
						appUri = u;
						break;
					}
				}
			}
			else {
				throw new NotImplementedException("Audience is not implemented. Could not create app link");
			}

			var uri = new UriBuilder(new Uri(appUri));
			if (!string.IsNullOrWhiteSpace(fragment)) {
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
			var audStr = HttpContext.User?.Claims?.FirstOrDefault(_ => _.Type == "aud")?.Value;
			if (audStr != null) {
				if (Enum.TryParse(audStr, true, out JwtAudience aud)) {
					return aud;
				}
			}
			return null;
		}


		[NonAction]
		protected async Task<DAL.Models.Identity.User> GetUserFromDb() {
			if (IsUserAuthenticated()) {
				var name = UserManager.NormalizeKey(HttpContext.User.Identity.Name);
				return await DbContext.Users
					.Include(_ => _.UserOptions)
					.Include(_ => _.UserVerification).ThenInclude(_ => _.LastKycTicket)
					.Include(_ => _.UserSumusWallet)
					.AsTracking()
					.FirstAsync(user => user.NormalizedUserName == name)
				;
			}
			return null;
		}

		[NonAction]
		protected UserAgentInfo GetUserAgentInfo() {
			// agent
			var agent = "Unknown";
			if (HttpContext.Request?.Headers.TryGetValue("User-Agent", out var agentParsed) ?? false) {
				agent = agentParsed.ToString();
			}

			return new UserAgentInfo() {
				Ip = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString(),
				IpObject = HttpContext.Connection.RemoteIpAddress.MapToIPv4(),
				Agent = agent,
			};
		}

		[NonAction]
		protected Locale GetUserLocale() {
			if (
				(HttpContext.Request?.Headers.TryGetValue("GM-Locale", out var localeHeader) ?? false) &&
				!string.IsNullOrWhiteSpace(localeHeader.ToString()) &&
				Enum.TryParse(localeHeader.ToString(), true, out Locale localeEnum)
			) {
				return localeEnum;
			}
			return Locale.En;
		}
		
		[NonAction]
		protected string GetUserCountry() {
			if (
				(HttpContext.Request?.Headers.TryGetValue("CF-IPCountry", out var hdr) ?? false) &&
				!string.IsNullOrWhiteSpace(hdr.ToString())
			) {
				return hdr.ToString().ToUpper();
			}
			return "XX";
		}

		[NonAction]
		protected async Task<UserTier> GetUserTier() {
			var user = await GetUserFromDb();
			return CoreLogic.User.GetTier(user);
		}
	}
}
