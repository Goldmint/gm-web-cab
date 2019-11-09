using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.WithdrawGoldModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/gold/withdraw")]
	public class WithdrawGoldController : BaseController {

		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized)]
		[HttpPost, Route("litewallet")]
		[ProducesResponseType(typeof(LiteWalletView), 200)]
		public async Task<APIResponse> LiteWallet([FromBody] LiteWalletModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var userLocale = GetUserLocale();
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			if (model.Amount <= 0.001m || user.UserSumusWallet.BalanceGold < model.Amount) {
				return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
			}

			// ---

			var timeNow = DateTime.UtcNow;

			// charge
			using (var scope = HttpContext.RequestServices.CreateScope()) {
				if (await CoreLogic.Finance.SumusWallet.Charge(scope.ServiceProvider, user.Id, model.Amount, SumusToken.Gold)) {
					
					try {
						var finHistory = new UserFinHistory() {
							Status = UserFinHistoryStatus.Processing,
							Type = UserFinHistoryType.GoldWithdraw,
							Source = "GOLD",
							SourceAmount = TextFormatter.FormatTokenAmountFixed(model.Amount),
							Destination = "",
							DestinationAmount = "",
							Comment = "",
							TimeCreated = timeNow,
							UserId = user.Id,
						};
						DbContext.UserFinHistory.Add(finHistory);
						await DbContext.SaveChangesAsync();

						var request = new WithdrawGold() {
							Status = EmissionRequestStatus.Requested,
							SumAddress = model.SumusAddress,
							Amount = model.Amount,
							TimeCreated = timeNow,
							UserId = user.Id,
							RelFinHistoryId = finHistory.Id,
						};
						DbContext.WithdrawGold.Add(request);
						await DbContext.SaveChangesAsync();

						// mint-sender service
						{
							var reply = await Bus.Request(
								MintSender.Subject.Sender.Request.Send,
								new MintSender.Sender.Request.Send() {
									Id = request.Id.ToString(),
									Amount = model.Amount.ToString("F18"),
									PublicKey = model.SumusAddress,
									Service = "core_gold_withdrawer",
									Token = "GOLD",
								},
								MintSender.Sender.Request.SendReply.Parser,
								3000
							);
							if (!reply.Success) {
								throw new Exception(reply.Error);
							}
						}

						return APIResponse.Success(
							new LiteWalletView() { }
						);
					} catch (Exception e) {
						try {
							await CoreLogic.Finance.SumusWallet.Refill(scope.ServiceProvider, user.Id, model.Amount, SumusToken.Gold);
						} catch { }
						Logger.Error(e, $"Failed to withdraw user {model.Amount} GOLD");
						return APIResponse.GeneralInternalFailure(e);
					}
				} else {
					return APIResponse.BadRequest(APIErrorCode.TradingNotAllowed);
				}
			}
		}
	}
}