using Goldmint.Common;
using Goldmint.CoreLogic.Services.Localization;
using Goldmint.CoreLogic.Services.Notification.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class SwiftController {

		/// <summary>
		/// Create swift deposit request
		/// </summary>
		[RequireJWTAudience(JwtAudience.App), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("deposit")]
		[ProducesResponseType(typeof(DepositView), 200)]
		public async Task<APIResponse> Deposit([FromBody] DepositModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			// round cents
			var amountCents = (long)Math.Floor(model.Amount * 100d);
			model.Amount = amountCents / 100d;

			// limits
			var transCurrency = FiatCurrency.USD;
			if (amountCents < AppConfig.Constants.SwiftData.DepositMin || amountCents > AppConfig.Constants.SwiftData.DepositMax) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// user
			var user = await GetUserFromDb();
			var userTier = CoreLogic.UserAccount.GetTier(user);
			var agent = GetUserAgentInfo();
			var userLocale = Locale.En;

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// new ticket
			var ticket = await TicketDesk.NewSwiftDeposit(user, transCurrency, amountCents);

			// make payment
			var request = new DAL.Models.SwiftRequest() {
				Type = SwiftPaymentType.Deposit,
				Status = SwiftPaymentStatus.Pending,
				Currency = transCurrency,
				AmountCents = amountCents,
				Holder = AppConfig.Constants.SwiftData.BenName.LimitLength(256),
				HolderAddress = AppConfig.Constants.SwiftData.BenAddress.LimitLength(512),
				Iban = AppConfig.Constants.SwiftData.BenIban.LimitLength(256),
				Bank = AppConfig.Constants.SwiftData.BenBankName.LimitLength(256),
				Bic = AppConfig.Constants.SwiftData.BenSwift.LimitLength(128),
				Details = AppConfig.Constants.SwiftData.BenBankAddress.LimitLength(1024),
				PaymentReference = "", // see below
				DeskTicketId = ticket,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
			};
			DbContext.SwiftRequest.Add(request);

			// history
			var finHistory = new DAL.Models.FinancialHistory() {
				Type = FinancialHistoryType.Deposit,
				AmountCents = amountCents,
				FeeCents = 0,
				DeskTicketId = ticket,
				Status = FinancialHistoryStatus.Success,
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
				Comment = "", // see below
			};
			DbContext.FinancialHistory.Add(finHistory);

			// save
			await DbContext.SaveChangesAsync();

			// update
			request.PaymentReference = $"D-{user.UserName}-{request.Id}";
			finHistory.Comment = $"Swift deposit request #{request.Id} ({request.PaymentReference})";
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.UserAccount.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: UserActivityType.Swift,
				comment: $"Swift deposit of {TextFormatter.FormatAmount(request.AmountCents, transCurrency)} requested ({request.PaymentReference})",
				ip: agent.Ip,
				agent: agent.Agent
			);

			// notification
			await EmailComposer.FromTemplate(await TemplateProvider.GetEmailTemplate(EmailTemplate.SwiftDepositInvoice, userLocale))

				.ReplaceBodyTag("BEN_NAME", AppConfig.Constants.SwiftData.BenName)
				.ReplaceBodyTag("BEN_ADDR", AppConfig.Constants.SwiftData.BenAddress)
				.ReplaceBodyTag("BEN_IBAN", AppConfig.Constants.SwiftData.BenIban)
				.ReplaceBodyTag("BEN_BANKNAME", AppConfig.Constants.SwiftData.BenBankName)
				.ReplaceBodyTag("BEN_BANKADDR", AppConfig.Constants.SwiftData.BenBankAddress)
				.ReplaceBodyTag("BEN_SWIFT", AppConfig.Constants.SwiftData.BenSwift)
				.ReplaceBodyTag("AMOUNT", TextFormatter.FormatAmount(amountCents, transCurrency))
				.ReplaceBodyTag("PAYMENT_REFERENCE", request.PaymentReference)
				
				.ReplaceBodyTag("REQID", request.Id.ToString())
				.Initiator(agent.Ip, agent.Agent, DateTime.UtcNow)
				.Send(user.Email, user.UserName, EmailQueue)
			;

			return APIResponse.Success(
				new DepositView() {
					BenName = request.Holder,
					BenAddress = request.HolderAddress,
					BenIban = request.Iban,
					BenBankName = request.Bank,
					BenBankAddress = request.Details,
					BenSwift = request.Bic,
					Reference = request.PaymentReference,
				}
			);
		}
	}
}
